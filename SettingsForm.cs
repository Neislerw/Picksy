using System;
using System.Windows.Forms;

namespace Picksy
{
    public partial class SettingsForm : Form
    {
        public SettingsForm(int currentBatchSize, int currentBatchTiming)
        {
            InitializeComponent();
            batchSizeNumericUpDown.Value = currentBatchSize;
            batchTimingNumericUpDown.Value = currentBatchTiming;
        }

        public int GetBatchSizeMinimum()
        {
            return (int)batchSizeNumericUpDown.Value;
        }

        public int GetBatchTimingMaximum()
        {
            return (int)batchTimingNumericUpDown.Value;
        }

        private void SaveButton_Click(object sender, EventArgs e)
        {
            int newBatchSize = (int)batchSizeNumericUpDown.Value;
            int newBatchTiming = (int)batchTimingNumericUpDown.Value;

            if (newBatchSize < 2 || newBatchSize > 100)
            {
                MessageBox.Show("Batch Size Minimum must be between 2 and 100.", "Invalid Input");
                return;
            }
            if (newBatchTiming < 1 || newBatchTiming > 600)
            {
                MessageBox.Show("Batch Timing Maximum must be between 1 and 600 seconds.", "Invalid Input");
                return;
            }

            DialogResult = DialogResult.OK;
            Close();
        }

        private void CancelButton_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.Cancel;
            Close();
        }
    }
}