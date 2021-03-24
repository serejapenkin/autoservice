using autoservice;
using autoservice.windows;
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


   
namespace autoservice
{
    public partial class Service
    {
        public Uri ImageUri
        {
            get
            {
                return new Uri((System.IO.Path.Combine(Environment.CurrentDirectory, MainImagePath ?? "")));
            }
        }
        public string CostString
        {
            get
            {
                // тут должно быть понятно - преобразование в строку с нужной точностью
                return Cost.ToString("#.##");
            }
        }

        public string CostWithDiscount
        {
            get
            {
                // Convert.ToDecimal - преобразует double в decimal
                // Discount ?? 0 - разнуливает "Nullable" переменную
                return (Cost * Convert.ToDecimal(1 - Discount ?? 0)).ToString("#.##");
            }
        }

        // ну и сразу пишем геттер на наличие скидки
        public Boolean HasDiscount
        {
            get
            {
                return Discount > 0;
            }
        }

        // и перечёркивание старой цены
        public string CostTextDecoration
        {
            get
            {
                return HasDiscount ? "None" : "Strikethrough";
            }
        }
        public double DiscountFloat
        {
            get
            {
                return Convert.ToSingle(Discount ?? 0);
            }
        }
        public string DescriptionString
        {
            get
            {
                return Description ?? "";
            }
        }
    }

    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        public MainWindow()
        {
            InitializeComponent();
            this.DataContext = this;
            ServiceList = Core.DB.Service.ToList();
        }

        private void ExitButton_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }
        private List<Service> _ServiceList;

        public List<Service> ServiceList
        {
            get
            {

                var FilteredServiceList = _ServiceList.FindAll(item =>
                item.DiscountFloat >= CurrentDiscountFilter.Item1 &&
                item.DiscountFloat < CurrentDiscountFilter.Item2);

                if (SearchFilter != "")
                    FilteredServiceList = FilteredServiceList.Where(item =>
                        item.Title.IndexOf(SearchFilter, StringComparison.OrdinalIgnoreCase) != -1 ||
                        item.DescriptionString.IndexOf(SearchFilter, StringComparison.OrdinalIgnoreCase) != -1).ToList();


                if (SortPriceAscending)
                    return FilteredServiceList.OrderBy(item => Double.Parse(item.CostWithDiscount)).ToList();

                else
                    return FilteredServiceList.OrderByDescending(item => Double.Parse(item.CostWithDiscount)).ToList();
            }
            set
            {
                _ServiceList = value;
                if (PropertyChanged != null)
                {
                    PropertyChanged(this, new PropertyChangedEventArgs("ServiceList"));
                }
            }
        }
        private Boolean _IsAdminMode = false;

        public event PropertyChangedEventHandler PropertyChanged;


        // публичный геттер, который меняет текущий режим (Админ/не Админ)
        public Boolean IsAdminMode
        {
            get { return _IsAdminMode; }
            set
            {
                _IsAdminMode = value;
                if (PropertyChanged != null)
                {
                    PropertyChanged(this, new PropertyChangedEventArgs("AdminModeCaption"));
                    PropertyChanged(this, new PropertyChangedEventArgs("AdminVisibility"));
                }

            }
        }
        // этот геттер возвращает текст для кнопки в зависимости от текущего режима
        public string AdminModeCaption
        {
            get
            {
                if (IsAdminMode) return "Выйти из режима\nАдминистратора";
                return "Войти в режим\nАдминистратора";
            }
        }
        private void AdminButton_Click(object sender, RoutedEventArgs e)
        {
            // если мы уже в режиме Администратора, то выходим из него 
            if (IsAdminMode) IsAdminMode = false;
            else
            {
                // создаем окно для ввода пароля
                var InputBox = new InputBoxWindow("Введите пароль Администратора");
                // и показываем его как диалог (модально)
                if ((bool)InputBox.ShowDialog())
                {
                    // если нажали кнопку "Ok", то включаем режим, если пароль введен верно
                    IsAdminMode = InputBox.InputText == "0000";
                }
            }
        }
        public string AdminVisibility
        {
            get
            {
                if (IsAdminMode) return "Visible";
                return "Collapsed";
            }
        }
        private Boolean _SortPriceAscending = true;
        public Boolean SortPriceAscending
        {
            get
            {
                return _SortPriceAscending;
            }
            set
            {
                _SortPriceAscending = value;
                if (PropertyChanged != null)
                {
                    PropertyChanged(this, new PropertyChangedEventArgs("ServiceList"));
                    PropertyChanged(this, new PropertyChangedEventArgs("ServicesCount"));
                    PropertyChanged(this, new PropertyChangedEventArgs("FilteredServicesCount"));

                }
            }
        }
        private void RadioButton_Checked(object sender, RoutedEventArgs e)
        {
            SortPriceAscending = (sender as RadioButton).Tag.ToString() == "1";
        }
        public List<string> FilterByDiscountNamesList
        {
            get
            {
                return FilterByDiscountValuesList
                    .Select(item => item.Item1)
                    .ToList();
            }

        }
        private List<Tuple<string, double, double>> FilterByDiscountValuesList =
            new List<Tuple<string, double, double>>()
            {

                Tuple.Create("Все записи", 0d, 1d),
                Tuple.Create("от 0% до 5%", 0d, 0.05d),
                Tuple.Create("от 5% до 15%", 0.05d, 0.15d),
                Tuple.Create("от 15% до 30%", 0.15d, 0.3d),
                Tuple.Create("от 30% до 70%", 0.3d, 0.7d),
                Tuple.Create("от 70% до 100%", 0.7d, 1d)
            };
        private Tuple<double, double> _CurrentDiscountFilter = Tuple.Create(double.MinValue, double.MaxValue);

        public Tuple<double, double> CurrentDiscountFilter
        {
            get
            {
                return _CurrentDiscountFilter;
            }
            set
            {
                _CurrentDiscountFilter = value;
                if (PropertyChanged != null)
                {
                    // при изменении фильтра список перерисовывается
                    PropertyChanged(this, new PropertyChangedEventArgs("ServiceList"));
                    PropertyChanged(this, new PropertyChangedEventArgs("ServicesCount"));
                    PropertyChanged(this, new PropertyChangedEventArgs("FilteredServicesCount"));
                }
            }
        }

        private string _SearchFilter = "";
        public string SearchFilter
        {
            get { return _SearchFilter; }
            set
            {
                _SearchFilter = value;
                if (PropertyChanged != null)
                {
                    // при изменении фильтра список перерисовывается
                    PropertyChanged(this, new PropertyChangedEventArgs("ServiceList"));
                    PropertyChanged(this, new PropertyChangedEventArgs("ServicesCount"));
                    PropertyChanged(this, new PropertyChangedEventArgs("FilteredServicesCount"));
                }
            }
        }

        private void TextBox_KeyUp(object sender, KeyEventArgs e)
        {
            SearchFilter = SearchFilterTextBox.Text;
        }

        private void DiscountFilterComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (DiscountFilterComboBox.SelectedIndex >= 0)
                CurrentDiscountFilter = Tuple.Create(
                    FilterByDiscountValuesList[DiscountFilterComboBox.SelectedIndex].Item2,
                    FilterByDiscountValuesList[DiscountFilterComboBox.SelectedIndex].Item3

                );
        }
        public int ServicesCount
        {
            get
            {
                return _ServiceList.Count;
            }

        }
        public int FilteredServicesCount
        {
            get
            {
                return ServiceList.Count;
            }
        }

        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            var item = MainDataGrid.SelectedItem as Service;

            // по условиям задачи нельзя удалять только те услуги, которые уже оказаны
            // свойство ClientService ссылается на таблицу оказанных услуг
            if (item.ClientService.Count > 0)
            {
                MessageBox.Show("Нельзя удалять услугу, она уже оказана");
                return;
            }

            // метод Remove нужно завернуть в конструкцию try..catch, на случай, если 
            // база спроектирована криво и нет каскадного удаления - это сделайте сами
            Core.DB.Service.Remove(item);

            // сохраняем изменения
            Core.DB.SaveChanges();

            // перечитываем изменившийся список, не забывая в сеттере вызвать PropertyChanged
            ServiceList = Core.DB.Service.ToList();
        }
        private void EditButton_Click(object sender, RoutedEventArgs e)
        {
            var SelectedService = MainDataGrid.SelectedItem as Service;
            var EditServiceWindow = new windows.ServiceWindow(SelectedService);
            if ((bool)EditServiceWindow.ShowDialog())
            {
                // при успешном завершении не забываем перерисовать список услуг
                PropertyChanged(this, new PropertyChangedEventArgs("ServiceList"));
                // и еще счетчики - их добавьте сами
            }
        }
        private void AddService_Click(object sender, RoutedEventArgs e)
        {
            // создаем новую услугу
            var NewService = new Service();

            var NewServiceWindow = new windows.ServiceWindow(NewService);
            if ((bool)NewServiceWindow.ShowDialog())
            {
                // список услуг нужно перечитать с сервера
                ServiceList = Core.DB.Service.ToList();
                PropertyChanged(this, new PropertyChangedEventArgs("FilteredProductsCount"));
                PropertyChanged(this, new PropertyChangedEventArgs("ProductsCount"));
            }
        }

        private void SubscrideButton_Click(object sender, RoutedEventArgs e)
        {
            var SelectedService = MainDataGrid.SelectedItem as Service;
            var SubscrideServiceWindow = new windows.ClientServiceWindow(SelectedService);
            SubscrideServiceWindow.ShowDialog();

        }

    }
}