﻿using PasswordEncryptor;
using SQLController;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace PasswordManager {
    public partial class frmNewUser : Form {
        #region Global Variables

        DataTable _userTable;
        string _usersUsername = "", _usersPassword = "";
        bool _tableContains = false;

        #endregion

        #region Constructors

        public frmNewUser() {
            InitializeComponent();
        }

        #endregion

        #region Button Events

        private void BtnCreate_Click(object sender, EventArgs e) {
            // Checking if the user has inputted values
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

            // Assign user input to the global variables
            _usersUsername = txtUsername.Text;
            _usersPassword = txtPassword.Text;

            // Get the user from the inputted username
            InitializeUserTable();

            // Checking if a user was found
            if (_tableContains) {
                MessageBox.Show($"A user by the username '{_usersUsername}' already exists",
                    Properties.Settings.Default.ProjectName,
                    MessageBoxButtons.OK);
                return;
            }

            // Converting the password to a hash
            _usersPassword = HashSalt.StringtoHashSalt(_usersPassword);

            // Adding a new row to User DataTable
            DataRow row = _userTable.NewRow();
            row["UserID"] = _userTable.Rows.Count + 1;
            row["Username"] = _usersUsername;
            row["PasswordHash"] = _usersPassword;
            _userTable.Rows.Add(row);


            // Create and assign the column names
            string columnNames = "UserID, Username, PasswordHash";

            // Create and assign the column values
            string columnValues = 
                $"{row["UserID"]}, " +
                $"'{row["Username"]}', " +
                $"'{row["PasswordHash"]}'";

            // Insert the record
            Context.InsertRecord("Users", columnNames, columnValues);

            // Close form
            Close();
        }

        #endregion

        #region Helper Methods

        private void InitializeUserTable() {
            // Create and assign a new SQL Query
            // Assign the User DataTable with the User Table
            _userTable = Context.GetDataTable("Users");
            foreach (DataRow row in _userTable.Rows) {
                if (row["Username"].Equals(_usersUsername)) {
                    _tableContains = true;
                }
            }
        }

        #endregion
    }
}
