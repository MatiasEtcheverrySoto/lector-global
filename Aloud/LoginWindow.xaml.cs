using System;
using System.Windows;
using System.Windows.Input;

namespace LectorGlobalApp
{
    public partial class LoginWindow : Window
    {
        private bool isRegisterMode = false;

        public LoginWindow(MainWindow main)
        {
            // Apply current dark mode theme to the new window BEFORE InitializeComponent
            foreach (System.Collections.DictionaryEntry entry in main.Resources)
            {
                this.Resources[entry.Key] = entry.Value;
            }
            
            InitializeComponent();
            this.DataContext = Strings.Instance;
            this.Owner = main;
        }

        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
                this.DragMove();
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void TxtToggleMode_Click(object sender, MouseButtonEventArgs e)
        {
            isRegisterMode = !isRegisterMode;
            if (isRegisterMode)
            {
                TxtToggleMode.Text = "¿Ya tienes cuenta? Inicia sesión";
                BtnSubmit.Content = "Registrarse";
            }
            else
            {
                TxtToggleMode.Text = Strings.Instance.LblCreateAccount;
                BtnSubmit.Content = Strings.Instance.BtnLoginAction;
            }
            TxtError.Visibility = Visibility.Collapsed;
        }

        private async void BtnSubmit_Click(object sender, RoutedEventArgs e)
        {
            string email = TxtEmail.Text.Trim();
            string pass = TxtPassword.Password;

            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(pass))
            {
                ShowError("Por favor completa todos los campos.");
                return;
            }

            BtnSubmit.IsEnabled = false;

            if (isRegisterMode)
            {
                // Extraer el nombre del correo antes del @
                string name = email.Split('@')[0];
                if (await AuthManager.Register(email, pass, name))
                {
                    this.Close();
                }
                else
                {
                    ShowError("Este correo ya está registrado o falló la creación.");
                }
            }
            else
            {
                if (await AuthManager.Login(email, pass))
                {
                    this.Close();
                }
                else
                {
                    ShowError("Correo o contraseña incorrectos.");
                }
            }

            BtnSubmit.IsEnabled = true;
        }

        private async void BtnGoogle_Click(object sender, RoutedEventArgs e)
        {
            if (await AuthManager.OAuthLogin("Google"))
            {
                this.Close();
            }
        }


        private void ShowError(string msg)
        {
            TxtError.Text = msg;
            TxtError.Visibility = Visibility.Visible;
        }
    }
}
