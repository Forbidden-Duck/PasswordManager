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
        DataTable _passwordTable;

        // Variables for Account Settings
        bool _userAdmin;
        DataView _dvUsers;
        DataTable _passwordsTable, _tagsTable;

        // Variables for the Tags List
        DataView _dvTag;
        DataTable _tagTable;

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
            panTagsList.Hide();
        }

        #endregion

        #region ToolStrip Events

        private void TsMain_ItemClicked(object sender, ToolStripItemClickedEventArgs e) {
            if (e.ClickedItem.Equals(tsbPasswordList)) {
                // PasswordList

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
                // AccountSettings

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
            } else if (e.ClickedItem.Equals(tsbTagsList)) {
                // TagsList

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
                _currentPanel = panTagsList;
                _currentButton = tsbTagsList;

                PopulateTagGrid();
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
        /// <summary>
        /// Used to Callback
        /// </summary>
        /// <param name="data">The callback parameters</param>
        delegate void PopulatePasswordGridCallBack(object data);
        private void ThreadProcPasswordImport(object data) {
            // Parse our new object as a DialogResult
            DialogResult result = (DialogResult)data;
            // Show Import Form (On a new thread to allow a STA Start
            // If form returns DialogResult OK
            // Populate Password DataGridView
            if (InvokeRequired) {
                if ((DialogResult)Invoke(new Func<DialogResult>(() => {
                    return new frmPasswordImporter(_userID, Location).ShowDialog();
                })) == DialogResult.OK) {
                    PopulatePasswordGridCallBack callback = new PopulatePasswordGridCallBack(ThreadProcPasswordImport);
                    Invoke(callback, DialogResult.OK);
                }
            } else {
                if (result == DialogResult.OK) {
                    PopulatePasswordGrid();
                }
            }
        }

        /// <summary>
        /// Start a new thread
        /// </summary>
        /// <param name="thread">The new thread</param>
        private void ThreadStart(Thread thread) {
            // Start the thread
            // Close the current form
            thread.Start();
            Close();
        }
        /// <summary>
        /// Start a new thread with STA
        /// </summary>
        /// <param name="thread">The new thread</param>
        private void ThreadStartSTA(Thread thread) {
            // Settting the Thread to STA to allow the use of Import Event
            thread.SetApartmentState(ApartmentState.STA);
            // Start the thread using our new object DialogResult.OK
            // Close the current form
            thread.Start(DialogResult.OK);
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
        /// Populates the DataGridView
        /// </summary>
        private void PopulatePasswordGrid() {
            // Create and assign a new SQL Query
            string sqlQuery =
                "SELECT * FROM Passwords " +
                $"WHERE UserID={_userID} " +
                "ORDER BY PasswordID DESC";

            // Create and assign the DataTable with the Password DataTable
            // Assign the DataTable to the DataView
            // Assign the DataGridView with the DataView
            DataTable passwordTable = Context.GetDataTable(sqlQuery, "Passwords");

            // Add a new column for holding TagDisplay
            DataColumn newColumn = new DataColumn();
            newColumn.ColumnName = "Tag";
            newColumn.DataType = typeof(string);
            newColumn.MaxLength = 120;
            passwordTable.Columns.Add(newColumn);

            // Setting the column names
            passwordTable.Columns[3].ColumnName = "Title";
            passwordTable.Columns[4].ColumnName = "Username";
            passwordTable.Columns[5].ColumnName = "Password";

            // For each row, changing TagID to TagDisplay
            foreach (DataRow row in passwordTable.Rows) {
                if (row["TagID"] is DBNull) {
                    row["Tag"] = "";
                } else {
                    string tagDisplay = GetTagDisplay(long.Parse(row["TagID"].ToString()));
                    if (string.IsNullOrEmpty(tagDisplay)) {
                        row["Tag"] = "";
                    } else {
                        row["Tag"] = tagDisplay;
                    }
                }
            }

            _dvPassword = new DataView(passwordTable);
            dgvPasswords.DataSource = _dvPassword;

            // Setting the UserID and TagID Column as invisible
            dgvPasswords.Columns[1].Visible = false;
            dgvPasswords.Columns[2].Visible = false;

            // Setting column 
            dgvPasswords.Columns[3].Width = 125;
            dgvPasswords.Columns[4].Width = 125;
            dgvPasswords.Columns[5].Width = 125;
            dgvPasswords.Columns[6].Width = 100;
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
        /// Returns the TagDisplay of a TagID
        /// </summary>
        /// <param name="tagID">The id of the tag</param>
        private string GetTagDisplay(long tagID) {
            // Create a variable for holding the string
            string tagDisplay = null;

            // Create and assign a new SQL Query
            // Assign the query to a temp datatable
            string sqlQuery =
                "SELECT TagID, TagDisplay FROM Tags " +
                $"WHERE TagID={tagID}";
            DataTable tagTable = Context.GetDataTable(sqlQuery, "Tags");

            // Check if the tag exists
            // Assign TagDisplay to the Local Variable
            if (tagTable.Rows.Count > 0) {
                tagDisplay = tagTable.Rows[0]["TagDisplay"].ToString();
            }

            // Return tagDisplay
            return tagDisplay;
        }

        #endregion

        #region PasswordList Events

        // TextBox Events
        private void TxtPasswordSearch_TextChanged(object sender, EventArgs e) {
            // Assign the DataView RowFilter
            _dvPassword.RowFilter =
                $"Title LIKE '%{txtPasswordSearch.Text}%' " +
                $"OR Username LIKE '%{txtPasswordSearch.Text}%'";
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
                        $"{row["Username"]};" +
                        $"{row["Password"]};" +
                        $"{row["TagID"]};" +
                        true);
                }
            } else {
                // For each DataRowView in Password DataView
                foreach (DataRowView row in _dvPassword) {
                    string userName = getUsername(long.Parse(row["UserID"].ToString()));
                    sbExport.AppendLine(
                        $"{userName};" +
                        $"{row["Title"]};" +
                        $"{row["Username"]};" +
                        $"{Encryption.Decrypt(row["Password"].ToString(), getUsername(long.Parse(row["UserID"].ToString())))};" +
                        $"{row["TagID"]};" +
                        false);
                }
            }

            // Write the StringBuilder to the PasswordManager CSV
            // Show a MessageBox
            File.WriteAllText(Application.StartupPath + $@"\{getUsername(_userID)}Passwords.csv", sbExport.ToString());
            MessageBox.Show("Passwords exported to CSV", Properties.Settings.Default.ProjectName);
        }
        private void BtnPasswordImport_Click(object sender, EventArgs e) {
            // Create a new thread using ParameterizedThreadStart (allows the paramters)
            ThreadStartSTA(new Thread(new ParameterizedThreadStart(ThreadProcPasswordImport)));
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
        /// Initialize the Tag DataTable
        /// </summary>
        private void InitializeTagTable(long userID) {
            // Create and assign a new SQL Query
            // Assign the Password DataTable with the Password Table
            string sqlQuery =
                "SELECT * FROM Tags " +
                $"WHERE UserID={userID}";
            _tagsTable = Context.GetDataTable(sqlQuery, "Tags");
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

        /// <summary>
        /// Deletes all the tags
        /// </summary>
        private void DeleteTags(long userID) {
            // Initialize the Tag DataTable
            InitializeTagTable(userID);
            if (_tagsTable.Rows.Count > 0) {
                // for each row in the Tag DataTable
                foreach (DataRow row in _tagsTable.Rows) {
                    // Delete the row
                    // Save the row
                    row.Delete();
                    row.EndEdit();
                }

                // Save Table
                Context.SaveDataBaseTable(_tagsTable);

                // Let the user know it was completed
                MessageBox.Show("Tags Deleted",
                    Properties.Settings.Default.ProjectName,
                    MessageBoxButtons.OK);
            } else {
                // Provide reason why
                MessageBox.Show("No tags to delete",
                    Properties.Settings.Default.ProjectName,
                    MessageBoxButtons.OK);
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
            } else {
                // Provide reason why
                MessageBox.Show("Cancelled, account not deleted",
                    Properties.Settings.Default.ProjectName,
                    MessageBoxButtons.OK);
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
            } else {
                // Provide reason why
                MessageBox.Show("Cancelled, no passwords deleted",
                    Properties.Settings.Default.ProjectName,
                    MessageBoxButtons.OK);
            }
        }
        private void BtnDeleteTags_Click(object sender, EventArgs e) {
            // Ask the user if they are sure
            DialogResult box = MessageBox.Show("Are you sure you want to delete your tags?",
                    Properties.Settings.Default.ProjectName,
                    MessageBoxButtons.YesNo);
            // If yes, continue on
            if (box == DialogResult.Yes) {
                // Delete the passwords
                DeleteTags(_userID);
            } else {
                // Provide reason why
                MessageBox.Show("Cancelled, no tags deleted",
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

        #region TagsList Helper Methods

        /// <summary>
        /// Initialize the Tag DataTable
        /// </summary>
        /// <param name="tagID"></param>
        private void InitializeTagInATable(long tagID) {
            // Create and assign a new SQL Query
            // Assign the Tag DataTable with the Tag Table
            string sqlQuery =
                "SELECT * FROM Tags " +
                $"WHERE TagID={tagID}";
            _tagTable = Context.GetDataTable(sqlQuery, "Tags");
        }

        /// <summary>
        /// Populates the DataGridView
        /// </summary>
        private void PopulateTagGrid() {
            // Create and assign a new SQL Query
            string sqlQuery =
                "SELECT * FROM Tags " +
                $"WHERE UserID={_userID} " +
                "ORDER BY TagID DESC";

            // Create and assign the DataTable with the Password DataTable
            // Assign the DataTable to the DataView
            // Assign the DataGridView with the DataView
            DataTable tagTable = Context.GetDataTable(sqlQuery, "Tags");
            // Setting the column names
            tagTable.Columns[2].ColumnName = "Display";
            tagTable.Columns[3].ColumnName = "Description";

            _dvTag = new DataView(tagTable);
            dgvTags.DataSource = _dvTag;

            // Setting the UserID Column as invisible
            dgvTags.Columns[1].Visible = false;

            // Setting column 
            dgvTags.Columns[2].Width = 185;
            dgvTags.Columns[3].Width = 250;
        }

        /// <summary>
        /// Initiates the Edit Event
        /// </summary>
        private void EditTagEvent() {
            // Create and assign the DataGridView Primary Key
            long tagID = long.Parse(dgvTags[0, dgvTags.CurrentCell.RowIndex].Value.ToString());

            // Create a new instance of frmTagModifier (with the Tag and User ID)
            // Set the forms parent to this form
            frmTagModifier frm = new frmTagModifier(tagID, _userID, Location);
            // Show the form
            // If the form returns DialogResult OK
            // Populate the DataGridView
            if (frm.ShowDialog() == DialogResult.OK) {
                PopulateTagGrid();
            }
        }

        #endregion

        #region TagsList Events

        // TextBox Events
        private void TxtTagsSearch_TextChanged(object sender, EventArgs e) {
            // Assign the DataView RowFilter
            _dvTag.RowFilter = $"Display LIKE '%{txtTagsSearch.Text}%' ";
        }

        // DataGridView Events
        private void DgvTags_DoubleClick(object sender, EventArgs e) {
            // Check if a cell has been selected in the DataGridView
            // If not then stop the method
            if (dgvTags.CurrentCell == null) {
                return;
            }

            EditTagEvent();
        }
        private void DgvTags_SelectionChanged(object sender, EventArgs e) {
            // Check if a cell has been selected
            // Enable the label
            // Else disable the label
            if (dgvTags.CurrentCell != null) {
                lblTagsWarning.Visible = false;
            } else {
                lblTagsWarning.Visible = true;
            }
        }

        // Button Events
        private void BtnNewTag_Click(object sender, EventArgs e) {
            // Create a new instance of frmTagModifier (with the User ID)
            // Set the forms parent to this form
            frmTagModifier frm = new frmTagModifier(_userID, Location);
            // Show the form
            // If the form returns DialogResult OK
            // Populate the DataGridView
            if (frm.ShowDialog() == DialogResult.OK) {
                PopulateTagGrid();
            }
        }
        private void BtnEditTag_Click(object sender, EventArgs e) {
            // Check if a cell has been selected in the DataGridView
            // If not then stop the method
            if (dgvTags.CurrentCell == null) {
                MessageBox.Show("No cell has been selected",
                    Properties.Settings.Default.ProjectName,
                    MessageBoxButtons.OK);
                return;
            }

            EditTagEvent();
        }
        private void BtnDeleteTag_Click(object sender, EventArgs e) {
            // Check if a cell has been selected in the DataGridView
            // If not then stop the method
            if (dgvTags.CurrentCell == null) {
                MessageBox.Show("No cell has been selected",
                    Properties.Settings.Default.ProjectName,
                    MessageBoxButtons.OK);
                return;
            }

            // Create and assign the tag id
            long tagID = long.Parse(dgvTags[0, dgvTags.CurrentCell.RowIndex].Value.ToString());
            // Assign the Tag DataTable with Tag Table
            InitializeTagInATable(tagID);

            // Delete the row from the table
            _tagTable.Rows[0].Delete();

            // Save the DataTable and Table
            _tagTable.Rows[0].EndEdit();
            Context.SaveDataBaseTable(_tagTable);

            // Re-populate the grid
            PopulateTagGrid();
        }

        #endregion
    }
}
