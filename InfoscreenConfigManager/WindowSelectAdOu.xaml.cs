using System;
using System.Collections.Generic;
using System.DirectoryServices;
using System.DirectoryServices.ActiveDirectory;
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
using System.Windows.Shapes;

namespace InfoscreenConfigManager {
	/// <summary>
	/// Логика взаимодействия для WindowSelectAdOu.xaml
	/// </summary>
	public partial class WindowSelectAdOu : Window {
		private string computerDomain;
		public string SelectedOU { get; set; } = string.Empty;
		private string previousSelectedOU;

		public WindowSelectAdOu(string previousSelectedOU) {
			InitializeComponent();
			this.previousSelectedOU = previousSelectedOU;

			computerDomain = Domain.GetComputerDomain().ToString();
			Loaded += WindowSelectAdOu_Loaded;
		}

		private TreeViewItem CreateTreeItem(ActiveDirectoryOu key) {
			TreeViewItem item = new TreeViewItem();
			item.Header = key.Name;
			item.Tag = key;
			item.Items.Add("Загрузка...");
			return item;
		}

		private void WindowSelectAdOu_Loaded(object sender, RoutedEventArgs e) {
			List<ActiveDirectoryOu> list = new List<ActiveDirectoryOu>();
			GetChildrenOu(string.Empty, ref list);

			foreach (ActiveDirectoryOu item in list)
				TreeViewMain.Items.Add(CreateTreeItem(item));

			if (string.IsNullOrEmpty(previousSelectedOU))
				return;
			
			FindParentFromDN(TreeViewMain.Items);
		}

		private void FindParentFromDN(ItemCollection collection) {
			foreach (TreeViewItem item in collection) {
				if (!(item.Tag is ActiveDirectoryOu))
					continue;

				string itemDN = (item.Tag as ActiveDirectoryOu).DistinguishedName;
				if (previousSelectedOU.EndsWith(itemDN)) {
					TreeViewMain_Expanded(TreeViewMain, new RoutedEventArgs(null, item));
					item.IsSelected = true;
					item.IsExpanded = true;
					FindParentFromDN(item.Items);
				}
			}
		}

		private void GetChildrenOu(string path, ref List<ActiveDirectoryOu> list) {
			string searchPath = "LDAP://";

			if (string.IsNullOrEmpty(path))
				searchPath += computerDomain;
			else
				searchPath += path;

			using (DirectoryEntry entry = new DirectoryEntry(searchPath)) {
				using (DirectorySearcher mySearcher = new DirectorySearcher(entry)) {
					mySearcher.SearchScope = SearchScope.OneLevel;
					mySearcher.Filter = ("(objectClass=organizationalUnit)");
					mySearcher.SizeLimit = int.MaxValue;
					mySearcher.PageSize = int.MaxValue;

					foreach (SearchResult resEnt in mySearcher.FindAll()) {
						try {
							string name = resEnt.GetDirectoryEntry().Properties["name"].Value.ToString();
							string dn = resEnt.GetDirectoryEntry().Properties["distinguishedName"].Value.ToString();

							list.Add(new ActiveDirectoryOu() { Name = name, DistinguishedName = dn });
						} catch (Exception exc) {
							Console.WriteLine(exc.Message + Environment.NewLine + exc.StackTrace);
						}
					}
				}
			}
		}

		private void TreeViewMain_Expanded(object sender, RoutedEventArgs e) {
			TreeViewItem item = e.Source as TreeViewItem;

			if ((item.Items.Count == 1) && (item.Items[0] is string)) {
				item.Items.Clear();

				if (item.Tag is ActiveDirectoryOu) {
					ActiveDirectoryOu activeDirectoryOu = item.Tag as ActiveDirectoryOu;
					List<ActiveDirectoryOu> list = new List<ActiveDirectoryOu>();
					GetChildrenOu(activeDirectoryOu.DistinguishedName, ref list);

					foreach (ActiveDirectoryOu ou in list)
						item.Items.Add(CreateTreeItem(ou));
				}
			}
		}

		private class ActiveDirectoryOu {
			public string Name { get; set; }
			public string DistinguishedName { get; set; }
		}

		private void ButtonSelect_Click(object sender, RoutedEventArgs e) {
			DialogResult = true;
			SelectedOU = ((TreeViewMain.SelectedItem as TreeViewItem).Tag as ActiveDirectoryOu).DistinguishedName;
			Close();
		}

		private void TreeViewMain_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e) {
			ButtonSelect.IsEnabled = TreeViewMain.SelectedItem != null;
		}
	}
}
