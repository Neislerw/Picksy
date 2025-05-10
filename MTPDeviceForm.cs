using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using System.Drawing;

namespace Picksy
{
    public partial class MTPDeviceForm : Form
    {
        private Button? selectFolderButton;
        private GroupBox? settingsGroupBox;
        private ComboBox? batchSelectionMethodComboBox;
        private Label? batchSelectionMethodLabel;
        private CheckBox? skipConfirmationCheckBox;
        private Label? batchTimingDescriptionLabel;
        private Label? batchSizeDescriptionLabel;
        private CheckBox? includeSubfoldersCheckBox;
        private NumericUpDown? batchTimingNumericUpDown;
        private Label? batchTimingLabel;
        private NumericUpDown? batchSizeNumericUpDown;
        private Label? batchSizeLabel;
        private Label? settingsHeaderLabel;
        private PictureBox? logoPictureBox;

        public MTPDeviceForm()
        {
            InitializeComponents();
        }

        private void InitializeComponents()
        {
            this.Size = new Size(800, 600);
            this.Text = "Picksy - Select Phone Folder";
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.StartPosition = FormStartPosition.CenterParent;

            logoPictureBox = new PictureBox
            {
                Location = new Point(350, 50),
                Size = new Size(300, 100),
                SizeMode = PictureBoxSizeMode.Zoom
            };
            try
            {
                logoPictureBox.Image = Image.FromFile("Resources\\logo.png");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading logo: {ex.Message}", "Picksy Error");
            }

            settingsGroupBox = new GroupBox
            {
                Location = new Point(175, 110),
                Size = new Size(450, 300)
            };

            settingsHeaderLabel = new Label
            {
                AutoSize = true,
                Font = new Font("Microsoft Sans Serif", 12F, FontStyle.Bold),
                Location = new Point(20, 20),
                Size = new Size(76, 20),
                Text = "Settings:"
            };

            batchSizeLabel = new Label
            {
                AutoSize = true,
                Location = new Point(20, 60),
                Size = new Size(139, 13),
                Text = "Batch Size Minimum (2–100):"
            };

            batchSizeNumericUpDown = new NumericUpDown
            {
                Location = new Point(300, 58),
                Maximum = 100,
                Minimum = 2,
                Size = new Size(60, 20),
                Value = 4
            };

            batchSizeDescriptionLabel = new Label
            {
                AutoSize = true,
                Font = new Font("Microsoft Sans Serif", 8.25F, FontStyle.Italic),
                Location = new Point(20, 80),
                Size = new Size(248, 26),
                Text = "The minimum number of closely timed photos to be\r\nconsidered a Batch"
            };

            batchTimingLabel = new Label
            {
                AutoSize = true,
                Location = new Point(20, 140),
                Size = new Size(154, 13),
                Text = "Batch Timing Maximum (1–600s):"
            };

            batchTimingNumericUpDown = new NumericUpDown
            {
                Location = new Point(300, 138),
                Maximum = 600,
                Minimum = 1,
                Size = new Size(60, 20),
                Value = 300
            };

            batchTimingDescriptionLabel = new Label
            {
                AutoSize = true,
                Font = new Font("Microsoft Sans Serif", 8.25F, FontStyle.Italic),
                Location = new Point(20, 160),
                Size = new Size(260, 39),
                Text = "The maximum amount of seconds between photos to\r\nstill be considered part of the same batch"
            };

            includeSubfoldersCheckBox = new CheckBox
            {
                AutoSize = true,
                Location = new Point(20, 220),
                Size = new Size(104, 17),
                Text = "Include Subfolders",
                UseVisualStyleBackColor = true
            };

            skipConfirmationCheckBox = new CheckBox
            {
                AutoSize = true,
                Location = new Point(20, 260),
                Size = new Size(179, 17),
                Text = "Skip Confirmation between batches",
                UseVisualStyleBackColor = true
            };

            batchSelectionMethodLabel = new Label
            {
                AutoSize = true,
                Location = new Point(20, 300),
                Size = new Size(132, 13),
                Text = "Batch Selection Method:"
            };

            batchSelectionMethodComboBox = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                FormattingEnabled = true,
                Location = new Point(300, 298),
                Size = new Size(130, 21)
            };
            batchSelectionMethodComboBox.Items.AddRange(new[] { "By Name", "By Date Created", "By Date Modified" });
            batchSelectionMethodComboBox.SelectedIndex = 0;

            settingsGroupBox.Controls.Add(settingsHeaderLabel);
            settingsGroupBox.Controls.Add(batchSizeLabel);
            settingsGroupBox.Controls.Add(batchSizeNumericUpDown);
            settingsGroupBox.Controls.Add(batchSizeDescriptionLabel);
            settingsGroupBox.Controls.Add(batchTimingLabel);
            settingsGroupBox.Controls.Add(batchTimingNumericUpDown);
            settingsGroupBox.Controls.Add(batchTimingDescriptionLabel);
            settingsGroupBox.Controls.Add(includeSubfoldersCheckBox);
            settingsGroupBox.Controls.Add(skipConfirmationCheckBox);
            settingsGroupBox.Controls.Add(batchSelectionMethodLabel);
            settingsGroupBox.Controls.Add(batchSelectionMethodComboBox);

            selectFolderButton = new Button
            {
                Location = new Point(325, 420),
                Size = new Size(150, 30),
                Text = "Select Phone Folder",
                UseVisualStyleBackColor = true
            };
            selectFolderButton.Click += new EventHandler(SelectFolderButton_Click);

            this.Controls.Add(logoPictureBox);
            this.Controls.Add(settingsGroupBox);
            this.Controls.Add(selectFolderButton);
        }

        private void SelectFolderButton_Click(object? sender, EventArgs e)
        {
            using (var dialog = new OpenFileDialog())
            {
                dialog.Title = "Select any photo in your phone's folder (e.g., This PC\\Your Phone\\DCIM)";
                dialog.Filter = "Image Files|*.jpg;*.jpeg;*.png";
                dialog.CheckFileExists = true;
                dialog.CheckPathExists = true;

                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        string selectedFile = dialog.FileName;
                        string? selectedFolder = Path.GetDirectoryName(selectedFile);
                        if (selectedFolder != null)
                        {
                            List<string> imageFiles = EnumerateImageFiles(selectedFolder);
                            MessageBox.Show($"Selected folder: {selectedFolder}\nFound {imageFiles.Count} image(s). File processing not yet integrated.", "Picksy");
                            // Placeholder: Integrate with PhotoGrouper here
                            this.Close();
                        }
                        else
                        {
                            MessageBox.Show("Could not determine the folder path.", "Picksy Error");
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Error accessing folder: {ex.Message}", "Picksy Error");
                    }
                }
            }
        }

        private List<string> EnumerateImageFiles(string folderPath)
        {
            var imageFiles = new List<string>();
            try
            {
                Guid iidShellFolder = new Guid("000214E6-0000-0000-C000-000000000046");
                if (SHParseDisplayName(folderPath, IntPtr.Zero, out IntPtr pidl, 0, out _) == 0)
                {
                    if (SHGetDesktopFolder(out IShellFolder desktopFolder) == 0)
                    {
                        if (desktopFolder.BindToObject(pidl, IntPtr.Zero, ref iidShellFolder, out IntPtr folderPtr) == 0)
                        {
                            IShellFolder folder = (IShellFolder)Marshal.GetObjectForIUnknown(folderPtr);
                            if (folder.EnumObjects(IntPtr.Zero, SHCONTF.SHCONTF_NONFOLDERS, out IEnumIDList enumItems) == 0)
                            {
                                IntPtr pidlItem;
                                while (enumItems.Next(1, out pidlItem, out uint fetched) == 0 && fetched == 1)
                                {
                                    folder.GetDisplayNameOf(pidlItem, SHGDN.SHGDN_FORPARSING, out STRRET name);
                                    string? filePath = GetStrRetString(name);
                                    if (filePath != null && IsImageFile(filePath))
                                    {
                                        imageFiles.Add(filePath);
                                    }
                                    Marshal.FreeCoTaskMem(pidlItem);
                                }
                                Marshal.ReleaseComObject(enumItems);
                            }
                            Marshal.ReleaseComObject(folder);
                        }
                        Marshal.ReleaseComObject(desktopFolder);
                    }
                    Marshal.FreeCoTaskMem(pidl);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error enumerating files: {ex.Message}", "Picksy Error");
            }
            return imageFiles;
        }

        private bool IsImageFile(string filePath)
        {
            string extension = Path.GetExtension(filePath).ToLower();
            return extension == ".jpg" || extension == ".jpeg" || extension == ".png";
        }

        private string? GetStrRetString(STRRET strRet)
        {
            if (strRet.uType == STRRET_TYPE.STRRET_WSTR)
            {
                return Marshal.PtrToStringUni(strRet.pOleStr);
            }
            return null;
        }

        // Shell API interop definitions
        [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
        private static extern int SHParseDisplayName([MarshalAs(UnmanagedType.LPWStr)] string pszName, IntPtr pbc, out IntPtr ppidl, uint sfgaoIn, out uint psfgaoOut);

        [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
        private static extern int SHGetDesktopFolder(out IShellFolder ppshf);

        [ComImport]
        [Guid("000214E6-0000-0000-C000-000000000046")]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        private interface IShellFolder
        {
            void ParseDisplayName(IntPtr hwnd, IntPtr pbc, [MarshalAs(UnmanagedType.LPWStr)] string pszDisplayName, out uint pchEaten, out IntPtr ppidl, ref uint pdwAttributes);
            int EnumObjects(IntPtr hwnd, SHCONTF grfFlags, out IEnumIDList ppenumIDList);
            void BindToObject(IntPtr pidl, IntPtr pbc, [In] ref Guid riid, out IntPtr ppv);
            void BindToStorage(IntPtr pidl, IntPtr pbc, [In] ref Guid riid, out IntPtr ppv);
            void CompareIDs(IntPtr lParam, IntPtr pidl1, IntPtr pidl2);
            void CreateViewObject(IntPtr hwndOwner, [In] ref Guid riid, out IntPtr ppv);
            void GetAttributesOf(uint cidl, [In] IntPtr[] apidl, ref uint rgfInOut);
            void GetUIObjectOf(IntPtr hwndOwner, uint cidl, [In] IntPtr[] apidl, [In] ref Guid riid, ref uint rgfReserved, out IntPtr ppv);
            void GetDisplayNameOf(IntPtr pidl, SHGDN uFlags, out STRRET pName);
            void SetNameOf(IntPtr hwnd, IntPtr pidl, [MarshalAs(UnmanagedType.LPWStr)] string pszName, SHCONTF uFlags, out IntPtr ppidlOut);
        }

        [ComImport]
        [Guid("000214F2-0000-0000-C000-000000000046")]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        private interface IEnumIDList
        {
            int Next(uint celt, out IntPtr rgelt, out uint pceltFetched);
            void Skip(uint celt);
            void Reset();
            void Clone(out IEnumIDList ppenum);
        }

        [StructLayout(LayoutKind.Explicit)]
        private struct STRRET
        {
            [FieldOffset(0)]
            public STRRET_TYPE uType;
            [FieldOffset(4)]
            public IntPtr pOleStr;
            [FieldOffset(4)]
            public uint uOffset;
            [FieldOffset(4)]
            public IntPtr cStr;
        }

        private enum STRRET_TYPE
        {
            STRRET_WSTR = 0,
            STRRET_OFFSET = 1,
            STRRET_CSTR = 2
        }

        private enum SHCONTF : uint
        {
            SHCONTF_FOLDERS = 0x0020,
            SHCONTF_NONFOLDERS = 0x0040
        }

        private enum SHGDN : uint
        {
            SHGDN_NORMAL = 0,
            SHGDN_FORPARSING = 0x8000
        }
    }
}