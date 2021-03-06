﻿using Encryptor;
using SQLController;
using System;
using System.Collections.Generic;
using System.Data;
using System.Threading;
using System.Windows.Forms;

namespace PasswordManager {
    public partial class frmLogin : Form {
        #region Global Variables

        // Create a variable for User DataTable
        // Create variables for User Input for Username and Password
        // Create a variable for the UserID
        /* 
         * Create a variable for checking the User DataTable
         * contains the User Input Username
        */
        DataTable _userTable;
        string _usersUsername = "", _usersPassword = "";
        int _usersAdmin = 0;
        long _userID = 0;
        bool _tableContains = false;

        #endregion

        #region Constructors

        /// <summary>
        /// Creates a new instance of our form
        /// </summary>
        public frmLogin() {
            // Initializes the form components
            InitializeComponent();

            // Hides the Create Panel
            panCreate.Hide();
            Text = "Login";
        }

        #endregion

        #region Form Events

        private void FrmLogin_Load(object sender, EventArgs e) {
            // Focus the form
            WindowState = FormWindowState.Minimized;
            WindowState = FormWindowState.Normal;
            Focus();
            Show();

            // Create and assign a new SQL Query
            string sqlQuery =
                "SELECT * FROM LoginDetails " +
                $"WHERE SessionID='{Properties.Settings.Default.SessionID}'";
            // Create and assign LoginDetails DataTable
            DataTable loginTable = Context.GetDataTable(sqlQuery, "LoginDetails");

            // Check if the StaySignedIn button was checked
            if (Properties.Settings.Default.StaySignedIn) {
                // If the DataTable contains rows
                // Login using the UserID
                if (loginTable.Rows.Count > 0) {
                    _userID = long.Parse(loginTable.Rows[0]["UserID"].ToString());
                    ThreadStart(new Thread(new ThreadStart(ThreadProcPasswordMain)));
                }
            }
        }

        private void FrmLogin_FormClosing(object sender, FormClosingEventArgs e) {
            // Check if Stay Signed In is check and a userID has been set
            if (cbSignedIn.Checked && _userID != 0) {
                // Create and assign LoginDetails DataTable
                DataTable loginTable = Context.GetDataTable("LoginDetails");

                // Grabbing all login details to create a new list of ids
                // This is to compare our generated id to the ones that already exist
                List<int> allLoginIDs = new List<int>();
                if (loginTable.Rows.Count > 0) {
                    foreach (DataRow loginRow in loginTable.Rows) {
                        allLoginIDs.Add(int.Parse(loginRow["LoginID"].ToString()));
                    }
                }

                // Creating a randomly generated number
                // Which will act as the SessionID
                // This will allow the user to auto login and is per application
                int randomID = GenerateRandomID();
                // Check if there are rows in the LoginTable
                // Check if the LoginID already exists
                if (loginTable.Rows.Count > 0) {
                    while (allLoginIDs.Contains(randomID)) {
                        // Update the randomID
                        randomID = GenerateRandomID();
                    }
                }

                // Set the user properties
                Properties.Settings.Default.SessionID = randomID;
                Properties.Settings.Default.StaySignedIn = true;
                Properties.Settings.Default.Save();

                // Create a new row in the DataTable
                // Change the UserID to the saved UserID
                // Save the row
                // Add the row
                DataRow row = loginTable.NewRow();
                row["LoginID"] = loginTable.Rows.Count + 1;
                row["SessionID"] = randomID;
                row["UserID"] = _userID;
                row["DateCreated"] = DateTime.Now;
                row.EndEdit();
                loginTable.Rows.Add(row);

                // Save the table
                Context.SaveDataBaseTable(loginTable);
            }
        }

        #endregion

        #region Button Events

        private void BtnLogin_Click(object sender, EventArgs e) {
            LoginMethod();
        }

        private void BtnCreate_Click(object sender, EventArgs e) {
            CreateMethod();
        }

        private void BtnExit_Click(object sender, EventArgs e) {
            // Closes the form
            Close();
        }

        private void BtnClose_Click(object sender, EventArgs e) {
            // Show the Login Panel
            panLogin.Show();
            Text = "Login";


            // Hide the Create Panel
            panCreate.Hide();
        }

        #endregion

        #region Key Events

        private void KeyEventLogin_KeyDown(object sender, KeyEventArgs e) {
            // Check if the user pressed Enter
            if (e.KeyCode == Keys.Enter) {
                // Login into the account
                // Set the DialogResult to OK
                LoginMethod();
                DialogResult = DialogResult.OK;
            } else if (e.Control && e.KeyCode == Keys.Back) {
                // Check if the user pressed CTRL+Backspace
                // Check if the sender was a TextBox
                if ((sender as TextBox) != null) {
                    // Supress the key press to prevent the char from showing
                    e.SuppressKeyPress = true;
                    // Check if the Selection Start is greater than 0
                    if ((sender as TextBox).SelectionStart > 0) {
                        // Delete everything to the left
                        SendKeys.Send("+{LEFT}{DEL}");
                    }
                }
            }
        }

        private void KeyEventCreate_KeyDown(object sender, KeyEventArgs e) {
            // Check if the user pressed Enter
            if (e.KeyCode == Keys.Enter) {
                // Create the account
                // Set the DialogResult to OK
                CreateMethod();
                DialogResult = DialogResult.OK;
            } else if (e.Control && e.KeyCode == Keys.Back) {
                // Check if the user pressed CTRL+Backspace
                // Check if the sender was a TextBox
                if ((sender as TextBox) != null) {
                    // Supress the key press to prevent the char from showing
                    e.SuppressKeyPress = true;
                    // Check if the Selection Start is greater than 0
                    if ((sender as TextBox).SelectionStart > 0) {
                        // Delete everything to the left
                        SendKeys.Send("+{LEFT}{DEL}");
                    }
                }
            }
        }

        private void KeyEvent_KeyPress(object sender, KeyPressEventArgs e) {
            // Check if the user pressed Enter
            if (e.KeyChar == (char)13) {
                // Set the key handled to true
                e.Handled = true;
            }
        }

        #endregion

        #region LinkLabel Events

        private void LnkNewUser_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e) {
            // Hide the Login Panel
            panLogin.Hide();

            // Show the Create Panel
            panCreate.Show();
            Text = "Create an account";
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// Runs a form
        /// </summary>
        private void ThreadProcPasswordMain() {
            // Show loading screen
            ThreadManage.ShowLoadingScreen();
            // Instantiate PasswordMain Form
            frmPasswordMain frm = new frmPasswordMain(_userID);
            // Close loading screen
            ThreadManage.CloseLoadingForm();
            // Run the form
            Application.Run(frm);
        }

        /// <summary>
        /// Starts a new thread
        /// </summary>
        /// <param name="thread">The new thread</param>
        private void ThreadStart(Thread thread) {
            // Start the new thread
            // Close the current form
            thread.Start();
            Close();
        }

        /// <summary>
        /// Initializes the User DataTable
        /// </summary>
        private void InitializeUserTable(bool isCreate) {
            // If isCreate = true
            // Create a editable DataTable
            // Else Get the a single user
            if (isCreate) {
                // Create and assign a new SQL Query
                // Assign the User DataTable with the User Table
                _userTable = Context.GetDataTable("Users");

                // For each row in the User DataTable
                // If the Username column in the row is equals to the User Input Username
                // Set the tableContains global variable to true
                foreach (DataRow row in _userTable.Rows) {
                    if (row["Username"].ToString().ToLower().Equals(_usersUsername.ToLower())) {
                        _tableContains = true;
                    }
                }
            } else {
                // Create and assign a new SQL Query
                // Assign the User DataTable with the User Table
                string sqlQuery =
                    "SELECT * FROM Users " +
                    $"WHERE Username='{_usersUsername}'";
                _userTable = Context.GetDataTable(sqlQuery, "Users", true);
            }
        }

        /// <summary>
        /// Runs the Login Code
        /// </summary>
        private void LoginMethod() {
            // Checking if the user has inputted values
            // Showing MessageBox providing reason
            if (string.IsNullOrEmpty(txtMainUsername.Text)) {
                MessageBox.Show("Please enter a username",
                    Properties.Settings.Default.ProjectName,
                    MessageBoxButtons.OK);
                return;
            } else if (string.IsNullOrEmpty(txtMainPassword.Text)) {
                MessageBox.Show("Please enter a password",
                    Properties.Settings.Default.ProjectName,
                    MessageBoxButtons.OK);
                return;
            }

            // Assign the User Inputted Username and Password to the Global Variables
            _usersUsername = txtMainUsername.Text;
            _usersPassword = txtMainPassword.Text;

            // Initialize the User DataTable
            InitializeUserTable(false);

            // Check if the username exists
            // Show a MessageBox
            if (_userTable.Rows.Count < 1) {
                MessageBox.Show($"No user by the username '{_usersUsername}' (case sensitive) was found",
                    Properties.Settings.Default.ProjectName,
                    MessageBoxButtons.OK);
                return;
            }

            // Creating a string for the password hash
            string userPasswordHash = _userTable.Rows[0]["PasswordHash"].ToString();

            // Comparing the users input to the hash password
            bool isEquals = HashSalt.CompareInputtoPassword(_usersPassword, userPasswordHash);

            // Checking if isEquals is true
            // Assign userID and starting PasswordList (with UserID)
            // Else Showing a MessageBox and resetting the variables
            if (isEquals) {
                _userID = long.Parse(_userTable.Rows[0]["UserID"].ToString());
                ThreadStart(new Thread(new ThreadStart(ThreadProcPasswordMain)));
            } else {
                MessageBox.Show("Password was incorrect",
                    Properties.Settings.Default.ProjectName,
                    MessageBoxButtons.OK);
                _usersUsername = "";
                _usersPassword = "";
                _userTable = null;
                return;
            }
        }

        /// <summary>
        /// Runs the Create Code
        /// </summary>
        private void CreateMethod() {
            // Checking if the user has inputted values
            // Showing a MessageBox with the provided reason
            if (string.IsNullOrEmpty(txtUsername.Text)) {
                MessageBox.Show("Please enter a username",
                    Properties.Settings.Default.ProjectName,
                    MessageBoxButtons.OK);
                return;
            } else if (string.IsNullOrEmpty(txtPassword.Text)) {
                MessageBox.Show("Please enter a password",
                    Properties.Settings.Default.ProjectName,
                    MessageBoxButtons.OK);
                return;
            }

            // Comparing the Password with Confirm Password
            // If not show a MessageBox
            if (!txtPassword.Text.Equals(txtConfirm.Text)) {
                MessageBox.Show("Confirm Password does not match Password",
                    Properties.Settings.Default.ProjectName,
                    MessageBoxButtons.OK);
                return;
            }

            // Assign the User Inputted Username and Password to the Global Variables
            _usersUsername = txtUsername.Text;
            _usersPassword = txtPassword.Text;

            // Initialize the User DataTable
            InitializeUserTable(true);

            // Check if there are rows in the User Table
            // Assign user as an Admin
            if (_userTable.Rows.Count < 1) {
                MessageBox.Show("You are the first user. You've been assigned as an Admin.",
                    Properties.Settings.Default.ProjectName,
                    MessageBoxButtons.OK);
                _usersAdmin = 1;
            }

            // Check if the username exists
            // Show a MessageBox
            if (_tableContains) {
                MessageBox.Show($"A user by the username '{_usersUsername}' already exists",
                    Properties.Settings.Default.ProjectName,
                    MessageBoxButtons.OK);
                return;
            }

            // Converting the inputted password to a Hash Salt
            _usersPassword = HashSalt.StringtoHashSalt(_usersPassword);

            // Adding a new row to User DataTable
            // Inputted the new data for the row before adding it
            // Save the DataTable
            DataRow row = _userTable.NewRow();
            row["UserID"] = _userTable.Rows.Count + 1;
            row["Username"] = _usersUsername;
            row["PasswordHash"] = _usersPassword;
            row["Admin"] = _usersAdmin;
            row["PasswordRequest"] = 0;
            row.EndEdit();
            _userTable.Rows.Add(row);

            // Save the Table
            Context.SaveDataBaseTable(_userTable);

            // Show the Login Panel
            panLogin.Show();
            Text = "Login";

            // Hide the Create Panel
            panCreate.Hide();
        }

        /// <summary>
        /// Generates a random 5 digit number
        /// </summary>
        /// <returns>A 5 digit int</returns>
        private int GenerateRandomID() {
            // Create a variable for our generated number
            int genNumber = 0;
            // Instantiate a new random number generator
            Random gen = new Random();
            // Assign a new generated number between 10,000 and 99,999
            genNumber = gen.Next(10000, 99999);
            // Return genNumber
            return genNumber;
        }

        #endregion
    }
}
