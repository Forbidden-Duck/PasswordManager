﻿using Encryptor;
using SQLController;
using System;
using System.Data;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace PasswordManager {
    public partial class frmPasswordImporter : Form {
        #region Global Variables

        long _userID = 0;
        DataTable _allPasswordsTable, _passwordsTable, _importTable;

        #endregion

        #region Constructors

        /// <summary>
        /// Creates a new instance of frmPasswordImporter
        /// </summary>
        /// <param name="userID">The users id</param>
        public frmPasswordImporter(long userID, Point location) {
            // Initalize the form components
            // Set frame location
            // Assign userID to Global Variable
            // Initialize the User's Passwords
            // Set ComboBox to -1 SelectedIndex
            InitializeComponent();
            Location = location;
            _userID = userID;
            InitializeUserPasswords();
            cbType.SelectedIndex = -1;
        }

        #endregion

        #region Button Events

        private void BtnImport_Click(object sender, EventArgs e) {
            // Check if PasswordManager was selected
            if (cbType.SelectedIndex == 0) {
                // Run PasswordManager Import Methods
                PasswordManagerImport();
            } else {
                // Otherwise, cancel
                MessageBox.Show("Cancelled. No item selected.",
                    Properties.Settings.Default.ProjectName,
                    MessageBoxButtons.OK);
            }

            // Close this thread
            Close();
        }

        #endregion

        #region Import Methods

        private void PasswordManagerImport() {
            // Import and read the CSV File
            PasswordManagerFileReader();
            // Check if the import came back with rows
            if (_importTable.Rows.Count < 1) {
                return;
            }

            // If the user has passwords. Ask if they want to remove them
            if (_passwordsTable.Rows.Count > 0) {
                // Ask the user if they want to remove the previous passwords
                DialogResult box = MessageBox.Show("Click Yes if you want the previous passwords deleted",
                        Properties.Settings.Default.ProjectName,
                        MessageBoxButtons.YesNo);

                // If the user clicked yes
                // Delete their passwords
                if (box == DialogResult.Yes) {
                    DeletePasswords();
                }
            }

            // Initialize the Passwords DataTable with the correct passwords
            InitializeAllPasswords();

            // For each row in the File Passwords
            foreach (DataRow row in _importTable.Rows) {
                // Create a new row
                DataRow newRow = _allPasswordsTable.NewRow();
                // Assign PasswordID, UserID and PasswordTitle
                newRow["PasswordID"] = int.Parse(_allPasswordsTable.Rows[_allPasswordsTable.Rows.Count - 1]["PasswordID"].ToString()) + 1;
                newRow["UserID"] = _userID;
                newRow["PasswordTitle"] = row["Title"];
                // Check if isEncrypted is false
                if (!bool.Parse(row["isEncrypted"].ToString())) {
                    // Encrypt the password
                    newRow["PasswordEncrypted"] = Encryption.Encrypt(row["Password"].ToString(), row["Username"].ToString());
                } else {
                    // Leave the password as is
                    newRow["PasswordEncrypted"] = row["Password"];
                }
                // Save the row
                newRow.EndEdit();
                _allPasswordsTable.Rows.Add(newRow);

                // Save the Table
                // Populate Grid
                Context.SaveDataBaseTable(_allPasswordsTable);
            }
        }

        #endregion

        #region File Readers

        /// <summary>
        /// Imports Passwords CSV File
        /// </summary>
        private void PasswordManagerFileReader() {
            // Create a new instance of DataTable
            DataTable PasswordsList = new DataTable();
            // Add the necessary columns to the DataTable
            PasswordsList.Columns.Add("Username");
            PasswordsList.Columns.Add("Title");
            PasswordsList.Columns.Add("Password");
            PasswordsList.Columns.Add("isEncrypted");

            // Try to read the lines in the file
            // Catch FileNotFound Exception
            try {
                // Create a new String Array containing all the lines
                string[] fileLines = File.ReadAllLines($"{getUsername()}Passwords.csv");

                // Check if there are any lines in the file
                if (fileLines.Length > 0) {
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
                            // This is to stop overwritting the Passwords Table
                            foreach (DataRow row in PasswordsList.Rows) {
                                row.Delete();
                            }
                        }
                        // Let the user know the read file failed
                        MessageBox.Show("Error: Reading the CSV File. Message: " + err.Message);
                    } finally {
                        // Check if there is rows
                        if (PasswordsList.Rows.Count > 0) {
                            // For each row in the PasswordsList
                            // Save the row
                            foreach (DataRow row in PasswordsList.Rows) {
                                row.EndEdit();
                            }
                        } else {
                            // Otherwise if no rows, tell user that it failed
                            MessageBox.Show("Failed to import passwords",
                                Properties.Settings.Default.ProjectName,
                                MessageBoxButtons.OK);
                        }
                    }
                } else {
                    // Otherwise, tell the user the CSV file is empty
                    MessageBox.Show("CSV File is empty",
                        Properties.Settings.Default.ProjectName,
                        MessageBoxButtons.OK);
                }
            } catch (FileNotFoundException) {
                // Tell the user the CSV File wasn't found
                MessageBox.Show("No exported CSV File found",
                        Properties.Settings.Default.ProjectName,
                        MessageBoxButtons.OK);
            }

            // Assign Import DataTable as PasswordsList
            _importTable = PasswordsList;
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// Initializes the User's Passwords DataTable
        /// </summary>
        private void InitializeUserPasswords() {
            // Create and assign a new SQL Query
            // Assign the Password DataTable with the Password Table
            string sqlQuery =
                "SELECT * FROM Passwords " +
                $"WHERE UserID={_userID} ";
            _passwordsTable = Context.GetDataTable(sqlQuery, "Passwords");
        }

        /// <summary>
        /// Initialize the All Passwords DataTable
        /// </summary>
        private void InitializeAllPasswords() {
            // Assign the All Passwords DataTable
            _allPasswordsTable = Context.GetDataTable("Passwords");
        }

        /// <summary>
        /// Delete the User's Passwords
        /// </summary>
        private void DeletePasswords() {
            // Check if the user has passwords
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
        /// Get the user's username
        /// </summary>
        /// <param name="userID">The provided UserID</param>
        /// <returns>Returns the username</returns>
        private string getUsername() {
            // Create and assign a new SQL Query
            // Assign a local User DataTable
            string sqlQuery =
                "SELECT UserID, Username FROM Users " +
                $"WHERE UserID={_userID}";
            DataTable userTable = Context.GetDataTable(sqlQuery, "Users");

            // Check if a Username was found
            // Return null
            // Else return the Username
            if (userTable.Rows.Count < 1) {
                return null;
            } else {
                return userTable.Rows[0]["Username"].ToString();
            }
        }

        #endregion
    }
}
