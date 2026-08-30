// SPDX-License-Identifier: GPL-3.0-only
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace PcmHacking
{
    public partial class MainForm
    {
        const int CellIndexEnable = 0;
        const int CellIndexZoom = 1;
        const int CellIndexParameter = 2;
        const int CellIndexUnits = 3;

        //private Dictionary<string, DataGridViewRow> parameterIdsToRows;
        private ParameterDatabase database = null!;
        private bool suspendSelectionEvents = true;

        /// <summary>
        /// Strongly-typed view over a parameterGrid row. Centralizes the cell-index math
        /// and unchecked casts that used to be scattered across this file.
        /// </summary>
        private readonly struct ParameterRow
        {
            private readonly DataGridViewRow row;

            public ParameterRow(DataGridViewRow row)
            {
                this.row = row;
            }

            public bool Enabled
            {
                get => this.row.Cells[CellIndexEnable].Value is bool value && value;
                set => this.row.Cells[CellIndexEnable].Value = value;
            }

            public bool Zoom
            {
                get => this.row.Cells[CellIndexZoom].Value is bool value && value;
                set => this.row.Cells[CellIndexZoom].Value = value;
            }

            public Parameter Parameter
            {
                get => (Parameter)this.row.Cells[CellIndexParameter].Value;
                set => this.row.Cells[CellIndexParameter].Value = value;
            }

            /// <summary>
            /// Same as <see cref="Parameter"/>, but returns null instead of throwing if the
            /// cell doesn't (yet) contain a Parameter.
            /// </summary>
            public Parameter? ParameterOrDefault => this.row.Cells[CellIndexParameter].Value as Parameter;

            public bool Visible
            {
                get => this.row.Visible;
                set => this.row.Visible = value;
            }

            private DataGridViewComboBoxCell UnitsCell => (DataGridViewComboBoxCell)this.row.Cells[CellIndexUnits];

            public void InitializeUnitsCell(IEnumerable<Conversion> conversions, Conversion defaultConversion)
            {
                DataGridViewComboBoxCell cell = this.UnitsCell;
                cell.DisplayMember = "Units";
                cell.ValueMember = "Units";

                foreach (Conversion conversion in conversions)
                {
                    cell.Items.Add(conversion);
                }

                cell.Value = defaultConversion;
            }

            public void SelectConversion(Conversion profileConversion, string profileUnits)
            {
                DataGridViewComboBoxCell cell = this.UnitsCell;
                foreach (Conversion conversion in cell.Items)
                {
                    if ((conversion == profileConversion) || (conversion.Units == profileUnits))
                    {
                        cell.Value = conversion;
                        return;
                    }
                }
            }

            public Conversion? GetSelectedConversion()
            {
                DataGridViewComboBoxCell cell = this.UnitsCell;
                string? selectedUnits = cell.Value as string;

                foreach (Conversion candidate in cell.Items)
                {
                    // The fact that we have to do both kinds of comparisons here really
                    // seems like a bug in the DataGridViewComboBoxCell code:
                    if ((candidate.Units == selectedUnits) || (candidate == cell.Value as Conversion))
                    {
                        return candidate;
                    }
                }

                return null;
            }
        }

        private IEnumerable<ParameterRow> ParameterRows =>
            this.parameterGrid.Rows.Cast<DataGridViewRow>().Select(row => new ParameterRow(row));

        private void FillParameterGrid()
        {
            // First, empty the grid.
            this.parameterGrid.Rows.Clear();

            // Not GetExecutingAssembly().Location: that is empty in the single-exe build, which
            // would make Path.GetDirectoryName throw.
            string appDirectory = AppContext.BaseDirectory;

            this.database = new ParameterDatabase(appDirectory);

            this.database.LoadDatabase();

            foreach (Parameter parameter in this.database.ListParametersBySupportedOs(osid))
            {
                DataGridViewRow gridRow = new DataGridViewRow();
                gridRow.CreateCells(this.parameterGrid);

                ParameterRow row = new ParameterRow(gridRow);
                row.Enabled = false;
                row.Zoom = false;
                row.Parameter = parameter;
                row.InitializeUnitsCell(parameter.Conversions, parameter.Conversions.First());

                this.parameterGrid.Rows.Add(gridRow);
            }

            this.suspendSelectionEvents = false;

            if (!this.parameterSearch.Focused)
            {
                this.ShowSearchPrompt();
            }
        }

        private void UpdateGridFromProfile()
        {
            try
            {
                this.suspendSelectionEvents = true;

                foreach (ParameterRow row in this.ParameterRows)
                {
                    row.Enabled = false;
                    row.Zoom = false;
                }

                foreach (LogColumn column in this.currentProfile.Columns)
                {
                    DataGridViewRow? gridRow = this.parameterGrid.Rows.Cast<DataGridViewRow>().FirstOrDefault(
                        r => new ParameterRow(r).Parameter == column.Parameter);

                    if (gridRow != null)
                    {
                        ParameterRow row = new ParameterRow(gridRow);
                        row.Enabled = true;
                        if (column.Zoom)
                        {
                            row.Zoom = true;
                        }

                        row.SelectConversion(column.Conversion, column.Conversion.Units);
                    }
                }
            }
            finally
            {
                this.suspendSelectionEvents = false;
            }
        }

        private void parameterGrid_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            // This ensures that checkbox changes are committed immediately.
            // By default they are on committed when focus leaves the cell.
            DataGridViewCheckBoxCell? checkBoxCell = this.parameterGrid.CurrentCell as DataGridViewCheckBoxCell;
            if ((checkBoxCell != null) && checkBoxCell.IsInEditMode && this.parameterGrid.IsCurrentCellDirty)
            {
                this.parameterGrid.EndEdit();
            }
        }

        private void parameterGrid_CurrentCellDirtyStateChanged(object sender, EventArgs e)
        {
            // Prevent the user from checking the Zoom box if the parameter is not
            // enabled. I had hoped to disable the Zoom boxes until the corresponding
            // parameter is enabled, but DataGridView doesn't support that.
            if (this.parameterGrid.CurrentCell.ColumnIndex == CellIndexZoom)
            {
                ParameterRow row = new ParameterRow(this.parameterGrid.Rows[this.parameterGrid.CurrentCell.RowIndex]);
                if (!row.Enabled)
                {
                    this.parameterGrid.CancelEdit();
                    return;
                }
            }

            // This ensures that checkbox changes are committed immediately.
            // By default they are on committed when focus leaves the cell.
            if (this.parameterGrid.IsCurrentCellDirty)
            {
                this.parameterGrid.CommitEdit(DataGridViewDataErrorContexts.Commit);
            }
        }

        private void parameterGrid_CellValueChanged(object sender, DataGridViewCellEventArgs e)
        {
            this.LogProfileChanged();
        }

        private void LogProfileChanged()
        { 
            if (this.suspendSelectionEvents)
            {
                return;
            }
 
            this.ResetProfile();

            this.ClearZoomPanel();

            this.CreateProfileFromGrid();

            this.SetDirtyFlag(true);
        }

        private void CreateProfileFromGrid()
        {
            this.ResetProfile();

            foreach (ParameterRow row in this.ParameterRows)
            {
                if (row.Enabled)
                {
                    Conversion? conversion = row.GetSelectedConversion();
                    LogColumn column = new LogColumn(row.Parameter, conversion!, row.Zoom);
                    this.currentProfile.AddColumn(column);
                }
            }
        }

        #region Parameter search
        private bool showSearchPrompt = true;

        private void ShowSearchPrompt()
        {
            this.parameterSearch.Text = "";
            parameterSearch_Leave(this, new EventArgs());
        }

        private void parameterSearch_Enter(object sender, EventArgs e)
        {
            if (this.showSearchPrompt)
            {
                this.parameterSearch.Text = "";
                this.showSearchPrompt = false;
                return;
            }
        }

        private void parameterSearch_Leave(object sender, EventArgs e)
        {
            if (this.parameterSearch.Text.Length == 0)
            {
                this.showSearchPrompt = true;
                this.parameterSearch.Text = "Search...";
                return;
            }
        }

        private void parameterSearch_TextChanged(object sender, EventArgs e)
        {
            if (this.showSearchPrompt)
            {
                return;
            }

            foreach (ParameterRow row in this.ParameterRows)
            {
                Parameter? parameter = row.ParameterOrDefault;
                if (parameter == null)
                {
                    continue;
                }

                row.Visible = parameter.Name.IndexOf(this.parameterSearch.Text, StringComparison.CurrentCultureIgnoreCase) != -1;
            }
        }
        #endregion
    }
}
