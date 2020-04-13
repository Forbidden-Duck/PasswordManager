﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace PasswordManager {
    public partial class InputDialog : Form {
        #region Return Variables

        public string InputValue {
            get; set;
        }

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes the InputDialog
        /// </summary>
        /// <param name="titleText">Form Title</param>
        /// <param name="descriptionText">Label Description</param>
        /// <param name="defaultText">TextBox Default</param>
        /// <param name="isPassword">Use System Char Boolean</param>
        public InputDialog(string titleText, string descriptionText, string defaultText, bool isPassword) {
            // Initialize the form components
            InitializeComponent();

            // Form Title
            // Label Description
            // Input Default
            // Input UseSystemPasswordChar
            Text = titleText;
            lblDescription.Text = descriptionText;
            txtInput.Text = defaultText;
            txtInput.UseSystemPasswordChar = isPassword;
        }

        #endregion

        #region Button Events

        private void BtnSubmit_Click(object sender, EventArgs e) {
            if (string.IsNullOrEmpty(txtInput.Text)) {
                DialogResult = DialogResult.None;
            } else {
                DialogResult = DialogResult.OK;
                InputValue = txtInput.Text;
            }
        }

        private void Close_Click(object sender, EventArgs e) {
            // Return Cancel
            // Close form
            DialogResult = DialogResult.Cancel;
            Close();
        }

        #endregion
    }
}