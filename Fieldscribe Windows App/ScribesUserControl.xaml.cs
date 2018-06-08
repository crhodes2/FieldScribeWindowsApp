using Fieldscribe_Windows_App.Controllers;
using Fieldscribe_Windows_App.Infrastructure;
using Fieldscribe_Windows_App.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Fieldscribe_Windows_App
{
    /// <summary>
    /// Interaction logic for ScribesUserControl.xaml
    /// </summary>
    public partial class ScribesUserControl : UserControl
    {
        private AppDataModel _appDataModel;
        private ScribesPanelDataModel _dataModel;
        private TokenManager _tokenManager;
        private bool _assignScribeSuccess;
        private bool _removeScribeSuccess;
        private User _selectedScribe;

        public ScribesUserControl()
        {
            InitializeComponent();

            _appDataModel = AppDataModel.Instance;
            _dataModel = ScribesPanelDataModel.Instance;
            _tokenManager = TokenManager.Instance;

            try
            {
                _dataModel.Scribes = GetAllScribes(new string[] { });
                RefreshScribesList(_dataModel.Scribes);
            }
            catch (Exception e)
            {
                // Will throw exception if not connected to internet
            }
        }

        private void UserControl_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if(this.Visibility == Visibility.Visible)
            {
                CollectionView view = (CollectionView)CollectionViewSource
                    .GetDefaultView(ScribesList.ItemsSource);

                view.Filter = ScribesFilter;

                ScribesTextFilter.Text = "";
            }
        }


        private bool ScribesFilter(object item)
        {
            if(String.IsNullOrEmpty(ScribesTextFilter.Text))
            {
                return true;
            }
            else
            {
                return ((item as User).FirstName.IndexOf(ScribesTextFilter.Text,
                    StringComparison.OrdinalIgnoreCase) >= 0 
                    || (item as User).LastName.IndexOf(ScribesTextFilter.Text,
                    StringComparison.OrdinalIgnoreCase) >= 0
                    || (item as User).Email.IndexOf(ScribesTextFilter.Text,
                    StringComparison.OrdinalIgnoreCase) >= 0);
            }
        }

        private void txtFilter_TextChanged(object sender, TextChangedEventArgs e)
        {
            CollectionViewSource.GetDefaultView(ScribesList.ItemsSource)
                .Refresh();
        }

        private void ScribesListBox_SelectionChanged(
            object sender, SelectionChangedEventArgs e)
        {
            // Check button enable conditions
            AddScribeBtn.IsEnabled = 
                (_appDataModel.MeetSelected && ScribesList.SelectedIndex >= 0
                && _dataModel.ScribesListSelected);
        }

        private void AssignedScribesListBox_SelectionChanged(
            object sender, SelectionChangedEventArgs e)
        {
            RemoveScribeBtn.IsEnabled = 
                (AssignedScribesList.SelectedIndex >= 0
                && _dataModel.AssignedScribesListSelected);
        }


        private void AddScribeBtn_Click(object sender, RoutedEventArgs e)
        {
            // Add as scribe
            _selectedScribe = (User)ScribesList.SelectedItem;

            // Try posting to API
            // Start Background worker thread

            BackgroundWorker worker = new BackgroundWorker();
            worker.DoWork += worker_AssignScribe;
            worker.RunWorkerCompleted += worker_AssignScribeComplete;
            worker.RunWorkerAsync();
        }

        private void worker_AssignScribe(object sender, DoWorkEventArgs e)
        {
            UsersController uc = new UsersController();

            if (_tokenManager.Token != "")
                _assignScribeSuccess = uc.AssignScribe(
                    _appDataModel.SelectedMeet.MeetId,
                    _selectedScribe.Id, _tokenManager.Token);

            else
            _assignScribeSuccess = false;
        }

        private void worker_AssignScribeComplete(object sender, RunWorkerCompletedEventArgs e)
        {
            if(_assignScribeSuccess)
            {
                _dataModel.Scribes.RemoveAt(ScribesList.SelectedIndex);
                RefreshScribesList(_dataModel.Scribes);

                _dataModel.AssignedScribes.Add(_selectedScribe);
                RefreshAssignedScribesList(_dataModel.AssignedScribes);
            }
        }

        private void RemoveScribeBtn_Click(object sender, RoutedEventArgs e)
        {
            _selectedScribe = (User)AssignedScribesList.SelectedItem;

            BackgroundWorker worker = new BackgroundWorker();
            worker.DoWork += worker_RemoveScribe;
            worker.RunWorkerCompleted += worker_RemoveScribeComplete;
            worker.RunWorkerAsync();
        }

        private void worker_RemoveScribe(object sender, DoWorkEventArgs e)
        {
            UsersController uc = new UsersController();

            if (_tokenManager.Token != "")
                _removeScribeSuccess = uc.RemoveScribe(
                    _appDataModel.SelectedMeet.MeetId,
                    _selectedScribe.Id, _tokenManager.Token);

            else
            _removeScribeSuccess = false;
        }

        private void worker_RemoveScribeComplete(object sender, RunWorkerCompletedEventArgs e)
        {
            if (_assignScribeSuccess)
            {
                _dataModel.AssignedScribes.RemoveAt(
                    AssignedScribesList.SelectedIndex);
                RefreshAssignedScribesList(_dataModel.AssignedScribes);

                _dataModel.Scribes.Add(_selectedScribe);
                RefreshScribesList(_dataModel.Scribes);
            }
        }

        private void RefreshScribesList(IList<User> scribes)
        {
            List<User> sortedList = scribes
                .OrderBy(scribe => scribe.LastName).ToList();

            ScribesList.ItemsSource = sortedList.Select(scribe => new User
            {
                Id = scribe.Id,
                FirstName = scribe.FirstName,
                LastName = scribe.LastName,
                Email = scribe.Email,
                CreatedAt = scribe.CreatedAt,
                Roles = scribe.Roles
            });
        }

        private void RefreshAssignedScribesList(IList<User> scribes)
        {
            List<User> sortedList = scribes
                .OrderBy(scribe => scribe.LastName).ToList();

            AssignedScribesList.ItemsSource = sortedList.Select(scribe => new User
            {
                Id = scribe.Id,
                FirstName = scribe.FirstName,
                LastName = scribe.LastName,
                Email = scribe.Email,
                CreatedAt = scribe.CreatedAt,
                Roles = scribe.Roles
            });
        }

        IList<User> GetAllScribes(string[] searchTerms)
        {
            UsersController uc = new UsersController();

            if (TokenManager.Instance.Token != "")
            {
                (bool success, IList<User> users) =
                    uc.GetScribes(searchTerms, TokenManager.Instance.Token);

                return users;
            }
            return null;
        }


        private void AssignedScribesList_MouseEnter(object sender, MouseEventArgs e)
        {
            _dataModel.AssignedScribesListSelected = true;

            RemoveScribeBtn.IsEnabled =
                (AssignedScribesList.SelectedIndex >= 0);

            AddScribeBtn.IsEnabled = false;
        }

        private void ScribesList_MouseEnter(object sender, MouseEventArgs e)
        {
            _dataModel.ScribesListSelected = true;

            AddScribeBtn.IsEnabled =
                (_appDataModel.MeetSelected 
                && ScribesList.SelectedIndex >= 0);

            RemoveScribeBtn.IsEnabled = false;
        }
    }
}
