﻿using Encryptor;
using SQLController;
using System;
using System.Data;
using System.IO;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace PasswordManager {
    public partial class frmPasswordMain : Form {
        #region Global Variables

        // Variables for Main Form
        long _userID = 0;
        DataTable _userTable;
        Panel _currentPanel = null;
        ToolStripButton _currentButton = null;

        // Variables for Password List
        DataView _dvPassword;
        DataTable _passwordTable, _allPasswordsTable;

        // Variables for Account Settings
        bool _userAdmin;
        DataView _dvUsers;
        DataTable _passwordsTable;

        #endregion

        #region Constructors

        /// <summary>
        /// Creates a new instance of PasswordMain
        /// </summary>
        /// <param name="userID">Provided User ID</param> 
        public frmPasswordMain(long userID) {
            // Initialize the form components
            // Assign the User ID with the provided User ID
            // Initialize the form
            InitializeComponent();
            _userID = userID;
            InitializeForm();
        }

        private void InitializeForm() {
            // Initialize the User DataTable
            InitializeUserTable();

            // Create the Account Combobox with Items
            // Set the SelectedIndex to 0
            tsdAccount.Text = $"Account: {_userTable.Rows[0]["Username"]}";
            tsdAccount.Select();

            // Hide every panel
            panPasswordList.Hide();
            panAccountSettings.Hide();
        }

        #endregion

        #region ToolStrip Events

        private void TsMain_ItemClicked(object sender, ToolStripItemClickedEventArgs e) {
            // TOOLSTRIP BUTTON - PASSWORDLIST
            if (e.ClickedItem.Equals(tsbPasswordList)) {
                // Check if there is a current shown panel
                // Hide the panel
                if (_currentPanel != null) {
                    _currentPanel.Hide();
                }
                // Check if there is a current active button
                // Unactive the button
                if (_currentButton != null) {
                    _currentButton.Checked = false;
                }
                // Set the current panel
                // Set the current button
                _currentPanel = panPasswordList;
                _currentButton = tsbPasswordList;

                // Populate the DataGrid on the Panel
                PopulatePasswordGrid();
            } else if (e.ClickedItem.Equals(tsbAccount)) {
                // Check if there is a current shown panel
                // Hide the panel
                if (_currentPanel != null) {
                    _currentPanel.Hide();
                }
                // Check if there is a current active button
                // Unactive the button
                if (_currentButton != null) {
                    _currentButton.Checked = false;
                }

                // Set the current panel
                // Set the current button
                _currentPanel = panAccountSettings;
                _currentButton = tsbAccount;

                // Check if the user is an admin
                // Disable save button
                isAdmin();
                btnAccountSave.Enabled = false;
            }
            // Set the current panel to show
            // Set the current button to active
            if (e.ClickedItem.GetType().Equals(typeof(ToolStripButton))) {
                _currentPanel.Show();
                _currentButton.Checked = true;
            }
        }

        #endregion

        #region ToolStripItem Events

        private void TsdAccount_DropDownItemClicked(object sender, ToolStripItemClickedEventArgs e) {
            // Checking if they clicked logout
            if (e.ClickedItem.Equals(tsdiLogout)) {
                // Logout
                Logout();
            }
        }

        #endregion

        #region Button Events

        private void BtnExit_Click(object sender, EventArgs e) {
            Close();
        }

        #endregion

        #region Main Helper Methods

        /// <summary>
        /// Run a form
        /// </summary>
        private void ThreadProcLogin() {
            Application.Run(new frmLogin());
        }
        private void ThreadProcPasswordImport() {
            Application.Run(new frmPasswordImporter(_userID, Location));
        }

        /// <summary>
        /// Start a new thread
        /// </summary>
        /// <param name="thread">The new thread</param>
        private void ThreadStart(Thread thread) {
            // Start the thread
            // Close the current form
            thread.Start();
            this.Close();
        }
        /// <summary>
        /// Start a new thread with STA
        /// </summary>
        /// <param name="thread">The new thread</param>
        private void ThreadStartSTA(Thread thread) {
            // Settting the Thread to STA to allow the use of Import Event
            thread.SetApartmentState(ApartmentState.STA);
            // Start the thread
            // Close the current form
            thread.Start();
            this.Close();
        }

        /// <summary>
        /// Initializes the User DataTable
        /// </summary>
        private void InitializeUserTable() {
            // Create and assign a new SQL Query
            // Assign the User DataTable with the User Table
            string sqlQuery =
                "SELECT * FROM Users " +
                $"WHERE UserID='{_userID}'";
            _userTable = Context.GetDataTable(sqlQuery, "Users", true);
        }

        /// <summary>
        /// Logs out of the form
        /// </summary>
        private void Logout() {
            // Create and assign LoginDetails DataTable
            DataTable loginTable = Context.GetDataTable("LoginDetails");

            // Check if the DataTable contains rows
            if (loginTable.Rows.Count > 0) {
                // For each row in the DataTable
                foreach (DataRow row in loginTable.Rows) {
                    // Delete the row
                    row.Delete();
                    // End any more editing on that row
                    row.EndEdit();
                }
                // Save the Table
                Context.SaveDataBaseTable(loginTable);
            }
            // Set stay signed in to false
            Properties.Settings.Default.StaySignedIn = false;
            Properties.Settings.Default.Save();

            // Create a new thread for frmMenu
            tsdAccount.Dispose();
            ThreadStart(new Thread(new ThreadStart(ThreadProcLogin)));
        }

        /// <summary>
        /// Get the user's username
        /// </summary>
        /// <param name="userID">The provided UserID</param>
        /// <returns>Returns the username</returns>
        private string getUsername(long userID) {
            // Create and assign a new SQL Query
            string sqlQuery =
                "SELECT UserID, Username FROM Users " +
                $"WHERE UserID={userID}";
            DataTable userTable = Context.GetDataTable(sqlQuery, "Users");

            if (userTable.Rows.Count < 1) {
                return null;
            } else {
                return userTable.Rows[0]["Username"].ToString();
            }
        }

        #endregion

        #region PasswordList Helper Methods

        /// <summary>
        /// Initialize the Password DataTable
        /// </summary>
        private void InitializePasswordInATable(long passwordID) {
            // Create and assign a new SQL Query
            // Assign the Password DataTable with the Password Table
            string sqlQuery =
                "SELECT * FROM Passwords " +
                $"WHERE PasswordID={passwordID}";
            _passwordTable = Context.GetDataTable(sqlQuery, "Passwords");


            // Create and assign the users username
            // Check if the username exists
            // Update the UserID in the Password DataTable
            string username = _userTable.Rows[0]["Username"].ToString();
            if (string.IsNullOrEmpty(username)) {
                MessageBox.Show("Couldn't find your username. Please login again.",
                    Properties.Settings.Default.ProjectName,
                    MessageBoxButtons.OK);
                return;
            }

            // Create and assign the decrypted password
            // Check if the password was decrypted properly
            // Update the PasswordEncrypted in the Password DataTable
            string passwordDecrypted = Encryption.Decrypt(_passwordTable.Rows[0]["PasswordEncrypted"].ToString(), username);
            if (string.IsNullOrEmpty(passwordDecrypted)) {
                MessageBox.Show("Failed to decrypt. Try again later.",
                    Properties.Settings.Default.ProjectName,
                    MessageBoxButtons.OK);
                return;
            }
            _passwordTable.Rows[0]["PasswordEncrypted"] = passwordDecrypted;
        }

        /// <summary>
        /// Initialize the All Passwords DataTable
        /// </summary>
        private void InitializeAllPasswords() {
            // Assign the All Passwords DataTable
            _allPasswordsTable = Context.GetDataTable("Passwords");
        }

        /// <summary>
        /// Populates the DataGridView
        /// </summary>
        private void PopulatePasswordGrid() {
            // Create and assign a new SQL Query
            string sqlQuery =
                "SELECT PasswordID, UserID, PasswordTitle, PasswordEncrypted " +
                $"FROM Passwords WHERE UserID={_userID}" +
                "ORDER BY PasswordID DESC";

            // Create and assign the DataTable with the Password DataTable
            // Assign the DataTable to the DataView
            // Assign the DataGridView with the DataView
            DataTable passwordTable = Context.GetDataTable(sqlQuery, "Passwords");
            // Setting the column names
            passwordTable.Columns[2].ColumnName = "Title";
            passwordTable.Columns[3].ColumnName = "Password";

            _dvPassword = new DataView(passwordTable);
            // Removing the UserID from the DataSource
            dgvPasswords.DataSource = _dvPassword.ToTable(
                "Selected", false,
                "PasswordID", "Title", "Password");

            // Setting column sizes
            dgvPasswords.Columns[1].Width = 150;
            dgvPasswords.Columns[2].Width = 150;
        }

        /// <summary>
        /// Initiates the Edit Event
        /// </summary>
        private void EditPasswordEvent() {
            // Create and assign the DataGridView Primary Key
            long passwordID = long.Parse(dgvPasswords[0, dgvPasswords.CurrentCell.RowIndex].Value.ToString());

            // Create a new instance of frmPasswordManager (with the Password and User ID)
            // Set the forms parent to this form
            frmPasswordModifier frm = new frmPasswordModifier(passwordID, _userID, Location);
            // Show the form
            // If the form returns DialogResult OK
            // Populate the DataGridView
            if (frm.ShowDialog() == DialogResult.OK) {
                PopulatePasswordGrid();
            }
        }

        /// <summary>
        /// Imports Passwords CSV File
        /// </summary>
        private DataTable PasswordFileReader() {
            // Instantiate a new OpenFileDialog
            OpenFileDialog fileDialog = new OpenFileDialog();
            // Set the Title of the dialog
            // Set the Filter of the dialog
            // Set the Initial Directory of the dialog
            fileDialog.Title = "Open CSV File";
            fileDialog.Filter = "CSV Files (*.csv)|*.csv";
            fileDialog.InitialDirectory = @"\";

            // Create a new instance of List String
            DataTable PasswordsList = new DataTable();
            // Show file dialog
            DialogResult fileResult = fileDialog.ShowDialog();
            // If file dialog returns OK
            if (fileResult == DialogResult.OK) {
                // Add the columns
                PasswordsList.Columns.Add("Username");
                PasswordsList.Columns.Add("Title");
                PasswordsList.Columns.Add("Password");
                PasswordsList.Columns.Add("isEncrypted");

                // Create a new String Array containing all the lines
                string[] fileLines = File.ReadAllLines(fileDialog.FileName);

                // Try to read the file
                // Catch any errors
                try {
                    // For each line in the file lines
                    foreach (string line in fileLines) {
                        // Create a new variable for containing all the entries on the lines
                        string[] lineSplit = line.Split(new string[] { ";" }, StringSplitOptions.None);
                        // Create variables for each entry
                        string Username = lineSplit[0];
                        string Title = lineSplit[1];
                        string Password = lineSplit[2];
                        string isEncrypted = lineSplit[3];

                        // Add the entries to the Passwords List
                        // Save row
                        PasswordsList.Rows.Add(Username, Title, Password, isEncrypted);
                    }
                } catch (Exception err) {
                    // Check if there is rows
                    if (PasswordsList.Rows.Count > 0) {
                        // For each row delete and save
                        // This is to stop the program
                        foreach (DataRow row in PasswordsList.Rows) {
                            row.Delete();
                        }
                    }
                    // Let the user know the read file failed
                    MessageBox.Show("Error: Reading the CSV File. Message: " + err.Message);
                } finally {
                    // Check if there is rows
                    if (PasswordsList.Rows.Count > 0) {
                        // For each row end edit
                        foreach (DataRow row in PasswordsList.Rows) {
                            row.EndEdit();
                        }
                    } else {
                        MessageBox.Show("Failed to import passwords",
                            Properties.Settings.Default.ProjectName,
                            MessageBoxButtons.OK);
                    }
                }
            } else if (fileResult == DialogResult.Cancel) {
                PasswordsList.Columns.Add("Cancelled");
            } else {
                PasswordsList.Columns.Add("Error");
            }

            // Return PasswordsList
            return PasswordsList;
        }

        #endregion

        #region PasswordList Events

        // TextBox Events
        private void TxtPasswordSearch_TextChanged(object sender, EventArgs e) {
            // Assign the DataView RowFilter
            _dvPassword.RowFilter = $"Title LIKE '%{txtPasswordSearch.Text}%'";
        }

        // DataGridView Events
        private void DgvPasswords_DoubleClick(object sender, EventArgs e) {
            // Check if a cell has been selected in the DataGridView
            // If not then stop the method
            if (dgvPasswords.CurrentCell == null) {
                return;
            }

            EditPasswordEvent();
        }
        private void DgvPasswords_SelectionChanged(object sender, EventArgs e) {
            // Check if a cell has been selected
            // Enable the label
            // Else disable the label
            if (dgvPasswords.CurrentCell != null) {
                lblPasswordWarning.Visible = false;
            } else {
                lblPasswordWarning.Visible = true;
            }
        }

        // Button Events
        private void BtnNewPassword_Click(object sender, EventArgs e) {
            // Create a new instance of frmPasswordManager (with the User ID)
            // Set the forms parent to this form
            frmPasswordModifier frm = new frmPasswordModifier(_userID, Location);
            // Show the form
            // If the form returns DialogResult OK
            // Populate the DataGridView
            if (frm.ShowDialog() == DialogResult.OK) {
                PopulatePasswordGrid();
            }
        }
        private void BtnEditPassword_Click(object sender, EventArgs e) {
            // Check if a cell has been selected in the DataGridView
            // If not then stop the method
            if (dgvPasswords.CurrentCell == null) {
                MessageBox.Show("No cell has been selected",
                    Properties.Settings.Default.ProjectName,
                    MessageBoxButtons.OK);
                return;
            }

            EditPasswordEvent();
        }
        private void BtnDeletePassword_Click(object sender, EventArgs e) {
            // Check if a cell has been selected in the DataGridView
            // If not then stop the method
            if (dgvPasswords.CurrentCell == null) {
                MessageBox.Show("No cell has been selected",
                    Properties.Settings.Default.ProjectName,
                    MessageBoxButtons.OK);
                return;
            }

            // Create and assign the password id
            long passwordID = long.Parse(dgvPasswords[0, dgvPasswords.CurrentCell.RowIndex].Value.ToString());
            // Assign the Password DataTable with Password Table
            InitializePasswordInATable(passwordID);

            // Delete the row from the table
            _passwordTable.Rows[0].Delete();

            // Save the DataTable and Table
            _passwordTable.Rows[0].EndEdit();
            Context.SaveDataBaseTable(_passwordTable);

            // Re-populate the grid
            PopulatePasswordGrid();
        }
        private void BtnPasswordExport_Click(object sender, EventArgs e) {
            if (_dvPassword.Table.Rows.Count < 1) {
                MessageBox.Show("No passwords to export",
                    Properties.Settings.Default.ProjectName,
                    MessageBoxButtons.OK);
                return;
            }

            // Ask the user if they want the passwords to be encrypted
            DialogResult box = MessageBox.Show("Click Yes for the exported passwords to be encrypted",
                    Properties.Settings.Default.ProjectName,
                    MessageBoxButtons.YesNo);
            // Create and assign a new StringBuilder
            StringBuilder sbExport = new StringBuilder();

            // Check if user clicked Yes
            if (box == DialogResult.Yes) {
                // For each DataRowView in Password DataView
                foreach (DataRowView row in _dvPassword) {
                    string userName = getUsername(long.Parse(row["UserID"].ToString()));
                    sbExport.AppendLine(
                        $"{userName};" +
                        $"{row["Title"]};" +
                        $"{row["Password"]};" +
                        true);
                }
            } else {
                // For each DataRowView in Password DataView
                foreach (DataRowView row in _dvPassword) {
                    string userName = getUsername(long.Parse(row["UserID"].ToString()));
                    sbExport.AppendLine(
                        $"{userName};" +
                        $"{row["Title"]};" +
                        $"{Encryption.Decrypt(row["Password"].ToString(), getUsername(long.Parse(row["UserID"].ToString())))};" +
                        false);
                }
            }

            // Write the StringBuilder to the PasswordManager CSV
            // Show a MessageBox
            File.WriteAllText(Application.StartupPath + $@"\{getUsername(_userID)}Passwords.csv", sbExport.ToString());
            MessageBox.Show("Passwords exported to CSV", Properties.Settings.Default.ProjectName);
        }
        private void BtnPasswordImport_Click(object sender, EventArgs e) {
            // Create a new thread for frmPasswordImporter
            tsdAccount.Dispose();
            ThreadStartSTA(new Thread(new ThreadStart(ThreadProcPasswordImport)));
        }

        #endregion

        #region AccountSettings Helper Methods

        /// <summary>
        /// Check if the user is an admin
        /// </summary>
        private void isAdmin() {
            gbAdminSettings.Visible = bool.Parse(_userTable.Rows[0]["Admin"].ToString());
            _userAdmin = bool.Parse(_userTable.Rows[0]["Admin"].ToString());
            PopulateAdminGrid();
        }

        /// <summary>
        /// Initialize the Password DataTable
        /// </summary>
        private void InitializePasswordTable(long userID) {
            // Create and assign a new SQL Query
            // Assign the Password DataTable with the Password Table
            string sqlQuery =
                "SELECT * FROM Passwords " +
                $"WHERE UserID={userID}";
            _passwordsTable = Context.GetDataTable(sqlQuery, "Passwords");
        }

        /// <summary>
        /// Gets all the Admins
        /// </summary>
        private int GetAllAdmins() {
            // Create and assign a new SQL Query
            // Create and assign the Admin DataTable
            string sqlQuery =
                "SELECT * FROM Users" +
                $"WHERE UserID!={_userID} AND Admin=1";
            DataTable adminTable = Context.GetDataTable(sqlQuery, "Users");

            // Return the adminTable Row Count
            return adminTable.Rows.Count;
        }

        /// <summary>
        /// Deletes all the passwords
        /// </summary>
        private void DeletePasswords(long userID) {
            // Initialize the Password DataTable
            InitializePasswordTable(userID);
            if (_passwordsTable.Rows.Count > 0) {
                // for each row in the Password DataTable
                foreach (DataRow row in _passwordsTable.Rows) {
                    // Delete the row
                    // Save the row
                    row.Delete();
                    row.EndEdit();
                }

                // Save Table
                Context.SaveDataBaseTable(_passwordsTable);
            }
        }

        /// <summary>
        /// Populates the User DataGridView
        /// </summary>
        private void PopulateAdminGrid() {
            // Create and assign a new SQL Query
            string sqlQuery =
                "SELECT UserID, Username, Admin " +
                $"FROM Users WHERE UserID!={_userID} " +
                "ORDER BY UserID DESC";

            // Create and assign the DataTable with the Password DataTable
            // Assign the DataTable to the DataView
            // Assign the DataGridView with the DataView
            DataTable userTable = Context.GetDataTable(sqlQuery, "Users");
            // Setting the column names
            userTable.Columns[1].ColumnName = "Username";
            userTable.Columns[2].ColumnName = "Admin";

            _dvUsers = new DataView(userTable);
            dgvUsers.DataSource = _dvUsers;

            // Setting column sizes
            dgvUsers.Columns[1].Width = 150;
            dgvUsers.Columns[2].Width = 55;
        }

        /// <summary>
        /// Initiates the Edit Event
        /// </summary>
        private void EditUserEvent() {
            // Create and assign the DataGridView Primary Key
            long userID = long.Parse(dgvUsers[0, dgvUsers.CurrentCell.RowIndex].Value.ToString());

            // Create a new instance of frmPasswordManager (with the Password and User ID)
            // Set the forms parent to this form
            frmUserModifier frm = new frmUserModifier(userID, Location);
            // Show the form
            // If the form returns DialogResult OK
            // Populate the DataGridView
            if (frm.ShowDialog() == DialogResult.OK) {
                PopulateAdminGrid();
            }
        }

        #endregion

        #region AccountSettings Events

        // User Settings
        private void BtnDeleteAccount_Click(object sender, EventArgs e) {
            // Check if the user is an admin
            if (_userAdmin) {
                // Check if they are the last admin
                if (GetAllAdmins() < 1) {
                    // Provide reason why
                    MessageBox.Show("You are the last admin. Can not delete your account",
                        Properties.Settings.Default.ProjectName,
                        MessageBoxButtons.OK);
                    return;
                }
            }

            // Ask the user if they are sure
            DialogResult box = MessageBox.Show("Are you sure you want to delete your account?",
                    Properties.Settings.Default.ProjectName,
                    MessageBoxButtons.YesNo);
            // If yes, continue on
            if (box == DialogResult.Yes) {
                // Delete the passwords
                DeletePasswords(_userID);

                // Delete user
                // Save DataTable
                // Save Table
                _userTable.Rows[0].Delete();
                _userTable.Rows[0].EndEdit();
                Context.SaveDataBaseTable(_userTable);

                // Logout
                Logout();
            }
        }
        private void BtnDeletePasswords_Click(object sender, EventArgs e) {
            // Ask the user if they are sure
            DialogResult box = MessageBox.Show("Are you sure you want to delete your passwords?",
                    Properties.Settings.Default.ProjectName,
                    MessageBoxButtons.YesNo);
            // If yes, continue on
            if (box == DialogResult.Yes) {
                // Delete the passwords
                DeletePasswords(_userID);

                // Let the user know it was completed
                MessageBox.Show("Passwords Deleted",
                    Properties.Settings.Default.ProjectName,
                    MessageBoxButtons.OK);
            } else {
                // Provide reason why
                MessageBox.Show("No passwords to delete",
                    Properties.Settings.Default.ProjectName,
                    MessageBoxButtons.OK);
            }
        }

        private void BtnChangePassword_Click(object sender, EventArgs e) {
            // Check the user entered a new password
            if (!string.IsNullOrEmpty(txtChangePassword.Text)) {
                // Compare the passwords
                // If isEqual = true
                bool isEqual = HashSalt.CompareInputtoPassword(txtChangePassword.Text, _userTable.Rows[0]["PasswordHash"].ToString());
                if (isEqual) {
                    // Provide reason why
                    MessageBox.Show($"\"{txtChangePassword.Text}\" is already your password",
                        Properties.Settings.Default.ProjectName,
                        MessageBoxButtons.OK);
                    // Reset the users input back to nothing
                    txtChangePassword.Text = "";
                    return;
                }

                // Converting the inputted password to a Hash Salt
                // Reset the users input back to nothing
                string usersPassword = HashSalt.StringtoHashSalt(txtChangePassword.Text);
                txtChangePassword.Text = "";

                // Adding the new password to the User DataTable
                _userTable.Rows[0]["PasswordHash"] = usersPassword;

                // Set the Save Buttno to enabled
                btnAccountSave.Enabled = true;
            } else {
                // Provide reason why
                MessageBox.Show("Please enter a password",
                    Properties.Settings.Default.ProjectName,
                    MessageBoxButtons.OK);
            }
        }
        private void BtnAccountSave_Click(object sender, EventArgs e) {
            // Saving User Table
            // Setting the Button Save back to disabled
            Context.SaveDataBaseTable(_userTable);
            btnAccountSave.Enabled = false;

            // Let the user know it was completed
            MessageBox.Show("Account Saved",
                Properties.Settings.Default.ProjectName,
                MessageBoxButtons.OK);
        }

        // Admin Settings
        private void DgvUsers_DoubleClick(object sender, EventArgs e) {
            // Check if a cell has been selected in the DataGridView
            // If not then stop the method
            if (dgvUsers.CurrentCell == null) {
                return;
            }

            EditUserEvent();
        }

        private void TxtUserSearch_TextChanged(object sender, EventArgs e) {
            // Assign the DataView RowFilter
            _dvUsers.RowFilter = $"Username LIKE '%{txtUserSearch.Text}%'";
        }

        private void BtnEditUser_Click(object sender, EventArgs e) {
            // Check if a cell has been selected in the DataGridView
            // If not then stop the method
            if (dgvUsers.CurrentCell == null) {
                MessageBox.Show("No cell has been selected",
                    Properties.Settings.Default.ProjectName,
                    MessageBoxButtons.OK);
                return;
            }

            EditUserEvent();
        }

        private void BtnDeleteUser_Click(object sender, EventArgs e) {
            // Check if a cell has been selected in the DataGridView
            // If not then stop the method
            if (dgvUsers.CurrentCell == null) {
                MessageBox.Show("No cell has been selected",
                    Properties.Settings.Default.ProjectName,
                    MessageBoxButtons.OK);
                return;
            }

            // Check if a cell has been selected in the DataGridView
            // If not then stop the method
            if (dgvUsers.CurrentCell == null) {
                MessageBox.Show("No cell has been selected",
                    Properties.Settings.Default.ProjectName,
                    MessageBoxButtons.OK);
                return;
            }

            // Create and assign the DataGridView Primary Key
            // Create and assign a new SQL Query
            // Create and assign a new User DataTable
            long userID = long.Parse(dgvUsers[0, dgvUsers.CurrentCell.RowIndex].Value.ToString());
            string sqlQuery =
                "SELECT * FROM Users " +
                $"WHERE UserID='{userID}'";
            DataTable userTable = Context.GetDataTable(sqlQuery, "Users");

            // Delete the users passwords
            DeletePasswords(userID);

            // Delete the row from the table
            userTable.Rows[0].Delete();

            // Save the DataTable and Table
            userTable.Rows[0].EndEdit();
            Context.SaveDataBaseTable(userTable);

            // Re-populate the grid
            PopulateAdminGrid();
        }

        #endregion
    }
}
