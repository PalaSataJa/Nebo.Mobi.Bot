using System;
using System.Net;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using Timer = System.Windows.Forms.Timer;

namespace Nebo.Mobi.Bot
{
	public class ManagerData
	{
		/// <summary>
		/// счетчик купленных товаров
		/// </summary>
		public int BuyCount { get; set; }

		/// <summary>
		/// счетчик собранных выручек (точнее этажей)
		/// </summary>
		public int CoinsCount { get; set; }

		/// <summary>
		/// счетчик перевезенных в лифте
		/// </summary>
		public int LiftCount { get; set; }

		/// <summary>
		/// счетчик выложенных товаров (точнее этажей)
		/// </summary>
		public int MerchCount { get; set; }
	}

	public class Manager
	{

		private readonly Timer _botTimer;
		public string ActionStatus = ""; //теекущее действие
		public string CommutationStr; //строка для логов
		public string ConnectStatus = ""; //статус соединения
		private string _homeLink; //ссылка на домашнюю страницу
		private string _html; //html-код текущей страницы
		private string _link; //переменная для обмена ссылками
		private string _login;
		private string _password;
		private readonly Random _rnd;
		public int Timeleft; //секунд до начала нового прохода

		public ManagerData ManagerData { get; private set; }

		private readonly WebClient _webClient;

		public Manager(Timer botTimer)
		{
			_botTimer = botTimer;
			_webClient = new WebClient();
			_rnd = new Random();
			ManagerData = new ManagerData();
		}

		public bool DoNotPut { get; set; }


		//проверяем, надо ли гонять лифт. возвращает ссылку на лифт если надо или пустоту если нет
		private string TryLift()
		{
			string[] str = _html.Split('\n');
			string ab = "";
			foreach (string a in str)
			{
				if (a.Contains(Constants.LiftEmpty)) //если пусто
					break;
				if (a.Contains(Constants.LiftFull) ||
						a.Contains(Constants.LiftVip)) //если есть народ или ВИПы
				{
					ab = a.Substring(21, 48);
					break;
				}
			}
			return ab;
		}


		//основной метод отправки лифта
		private void GoneLift()
		{
			//проверяем, надо ли гнать лифт
			string ab = TryLift();

			ManagerData.LiftCount = 0;
			if (ab != "")
			{
				ActionStatus = "   -   Катаю лифт";

				while (ab != "")
				{
					ab = GetLiftLink(ab);
					Thread.Sleep(_rnd.Next(351, 1500));
				}
				ActionStatus = "";
				CommutationStr = string.Format("{0}  -  Доставлено пассажиров: {1}.\n", GetTime(), ManagerData.LiftCount);
			}
			Thread.Sleep(_rnd.Next(30, 100));
		}

		//проверка есть ли выручка
		private string TryMoney()
		{
			string ab = Parse(_html, "Собрать выручку!");
			if (ab != "")
			{
				ab = ab.Substring(114);
				ab = ab.Remove(ab.IndexOf('\"'));
			}
			return ab;
		}

		//переход поссылке сбора выручки и получения новой ссылки сбора выручки
		private string GetMoneyLink(string lnk)
		{
			try
			{
				ClickLink(lnk, "");
			}
			catch (Exception ex)
			{
				ThreadAbort("ОШИБКА. " + ex.Message + '\n');
			}

			string ab = Parse(_html, "Собрать выручку!");
			if (ab != "")
			{
				ab = ab.Substring(114);
				ab = ab.Remove(ab.IndexOf('\"'));
				ManagerData.CoinsCount++;
			}
			return ab;
		}

		//базовый меод сбора выручки
		private void CollectMoney()
		{
			//ищем ссылку сбора выручки
			string ab = TryMoney();
			ManagerData.CoinsCount = 0;
			if (ab != "")
			{
				ActionStatus = "   -   Собираю выручку";
				ManagerData.CoinsCount = 1;
				while (ab != "")
				{
					ab = GetMoneyLink(ab);
					Thread.Sleep(_rnd.Next(351, 1500));
				}

				ActionStatus = "";
				CommutationStr = string.Format("{0}  -  Этажей, с которых собрана выручка: {1}.\n", GetTime(), ManagerData.CoinsCount);
			}
			Thread.Sleep(_rnd.Next(30, 100));
		}

		//проверяем есть ли чего выложить
		private string TryPutMerch()
		{
			string ab = Parse(_html, "Выложить товар");
			if (ab != "")
			{
				ab = ab.Substring(117);
				ab = ab.Remove(ab.IndexOf('\"'));
			}
			return ab;
		}

		//переход поссылке сбора выручки и получения новой ссылки сбора выручки
		private string GetMerchLink(string lnk)
		{
			try
			{
				ClickLink(lnk, "");
			}
			catch (Exception ex)
			{
				ThreadAbort("ОШИБКА. " + ex.Message + '\n');
			}

			string ab = Parse(_html, "Выложить товар");
			if (ab != "")
			{
				ab = ab.Substring(117);
				ab = ab.Remove(ab.IndexOf('\"'));
				ManagerData.MerchCount++;
			}
			return ab;
		}

		//базовый меод сбора выручки
		private void PutMerch()
		{
			//ищем ссылку сбора выручки
			string ab = TryPutMerch();
			ManagerData.MerchCount = 0;
			if (ab != "")
			{
				ActionStatus = "   -   Выкладываю товар";
				ManagerData.MerchCount = 1;
				while (ab != "")
				{
					ab = GetMerchLink(ab);
					Thread.Sleep(_rnd.Next(351, 1500));
				}

				ActionStatus = "";
				CommutationStr = string.Format("{0}  -  Этажей, на которых выложен товар: {1}.\n", GetTime(), ManagerData.MerchCount);
			}
			Thread.Sleep(_rnd.Next(30, 100));
		}


		//проверяем есть ли чего закупить
		private string TryBuy()
		{
			string ab = "";
			GetHomePage();

			//простейший случай - постоянный товарооборот
			if (!DoNotPut)
			{
				ab = Parse(_html, "Закупить товар");
				if (ab != "")
				{
					ab = ab.Substring(120);
					ab = ab.Remove(ab.IndexOf('\"'));
				}
			}

				//а теперь если ждем инвесторов и ничего не выкладываем
			else
			{
				string[] str = _html.Split('\n');
				int i;
				for (i = 0; i < str.Length; i++)
				{
					//если есть чего закупать и нечего не доставляется
					if (str[i].Contains("st_empty.png") && !(str[i].Contains("st_stocking.png")))
						break;
				}

				if (i != str.Length) //типа если нашлось
				{
					//переходим к строке с ссылкой на этаж
					i += 4;

					//получаем строку с сылкой на этаж
					ab = str[i];
					ab = ab.Substring(9);
					ab = ab.Remove(ab.IndexOf('\"'));
				}
			}
			return ab;
		}

		//переход поссылке сбора выручки и получения новой ссылки сбора выручки
		private string GetBuyLink(string lnk)
		{
			try
			{
				ClickLink(lnk, "");
			}
			catch (Exception ex)
			{
				ThreadAbort("ОШИБКА. " + ex.Message + '\n');
			}

			string[] str = _html.Split('\n');
			string ab = "";
			foreach (string ss in str)
			{
				//вычленяем ссылку на самый дорогой
				if (ss.Contains("Закупить за"))
				{
					ab = ss.Substring(21);
					ab = ab.Remove(ab.IndexOf('\"'));
				}
			}

			//сама закупка
			Thread.Sleep(_rnd.Next(300, 1000));
			try
			{
				ClickLink(ab, "");
			}
			catch (Exception ex)
			{
				ThreadAbort("ОШИБКА. " + ex.Message + '\n');
			}

			ab = TryBuy();
			return ab;
		}

		//получение полного списка этажей
		private void GoHome()
		{
			//подключаемся и переходим на Главную
			Connect();

			//получаем ссылку "Показать этажи"
			string ab = Parse(_html, "Показать этажи");
			if (ab != "")
			{
				ab = ab.Substring(49);
				ab = ab.Remove(ab.IndexOf("\"", StringComparison.Ordinal));

				try
				{
					ClickLink(ab, "");
				}
				catch (Exception ex)
				{
					ThreadAbort("ОШИБКА. " + ex.Message + '\n');
				}
			}
		}

		private string GetTime()
		{
			return string.Format(@"{0:d2}.{1:d2}.{2:d4}  [{3:d2}:{4:d2}:{5:d2}]",
				DateTime.Now.Day, DateTime.Now.Month, DateTime.Now.Year, DateTime.Now.Hour, DateTime.Now.Minute, DateTime.Now.Second);
		}


		//список дел бота
		public void StartBot(bool doNotPut, int mintime, int maxtime, string login, string password)
		{
			DoNotPut = doNotPut;
			_login = login;
			_password = password;
			//сбрасываем таймер обратного отсчета
			Timeleft = 0;
			//подключаеся, идем на главную, раскрываем этажи
			GoHome();

			//делаем 2 прогона (мб что-то доставят или купят випы)
			for (int i = 0; i < 2; i++)
			{
				CollectMoney();
				if (!DoNotPut) PutMerch();
				Buy();
				GoneLift();
			}

			//получаем рандомное время ожидания
			_botTimer.Interval = _rnd.Next(mintime, maxtime);
			_botTimer.Interval = (int)(_botTimer.Interval * 0.001) * 1000;
			ConnectStatus = "   -   Стоп";
			Timeleft = (int)(_botTimer.Interval * 0.001);
			CommutationStr = string.Format("Жду   {0}",
				string.Format("{0}мин : {1:d2}сек\n\n", Timeleft / 60, Timeleft - (Timeleft / 60 * 60)));
		}


		//парсит страницу, возвращает строку с ссылкой по маске
		private string Parse(string page, string mask)
		{
			string[] str = page.Split('\n');
			string ab = "";
			foreach (string a in str)
			{
				if (a.Contains(mask))
				{
					ab = a;
					break;
				}
			}
			return ab;
		}

		//тупо получение главного экрана
		private void GetHomePage()
		{
			try
			{
				ClickLink(_homeLink, "");
			}
			catch (Exception ex)
			{
				ThreadAbort("ОШИБКА. " + ex.Message + '\n');
			}

			//получаем ссылку "Показать этажи"
			string ab = Parse(_html, "Показать этажи");
			if (ab != "")
			{
				ab = ab.Substring(49);
				ab = ab.Remove(ab.IndexOf("\"", StringComparison.Ordinal));

				try
				{
					ClickLink(ab, "");
				}
				catch (Exception ex)
				{
					ThreadAbort("ОШИБКА. " + ex.Message + '\n');
				}
			}
		}

		//базовый меод закупки товара
		private void Buy()
		{
			//ищем ссылку сбора выручки
			string ab = TryBuy();
			ManagerData.BuyCount = 0;
			if (ab != "")
			{
				ActionStatus = "   -   Закупаю товар";
				while (ab != "")
				{
					ab = GetBuyLink(ab);
					ManagerData.BuyCount++;
					Thread.Sleep(_rnd.Next(351, 1500));
				}

				ActionStatus = "";
				CommutationStr = string.Format("{0}  -  Этажей, на которых закуплен товар: {1}.\n", GetTime(), ManagerData.BuyCount);
			}
			Thread.Sleep(_rnd.Next(30, 100));
		}


		//катаем лифт и получаем очередную ссылку
		private string GetLiftLink(string lnk)
		{
			try
			{
				ClickLink(lnk, "");
			}
			catch (Exception ex)
			{
				ThreadAbort("ОШИБКА. " + ex.Message + '\n');
			}

			string ab = Parse(_html, "Поднять");
			if (ab != "")
			{
				ab = ab.Substring(111);
				if (ab.IndexOf('\"') != -1) ab = ab.Remove(ab.IndexOf('\"'));
			}
			else
			{
				ab = Parse(_html, "Получить");
				if (ab != "")
				{
					ab = ab.Substring(27);
					if (ab.IndexOf('\"') != -1) ab = ab.Remove(ab.IndexOf('\"'));
					ManagerData.LiftCount++;
				}
			}
			return ab;
		}

		//метод сброса потока бота
		private void ThreadAbort(string reason)
		{
			CommutationStr = reason;
			ConnectStatus = "";
			ActionStatus = "";
			Thread.CurrentThread.Abort();
		}

		//входим на домашнюю станицу получаем ссылку на форму входа
		private void Entery()
		{
			ConnectStatus = "  -  Подключение к серверу";
			try
			{
				ClickLink("login", "");
			}
			catch (Exception ex)
			{
				ThreadAbort("ОШИБКА. " + ex.Message + '\n');
			}

			_link = Parse(_html, "<form action=");
			try
			{
				_link = _link.Substring(14, 107);
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.Message);
			}
		}

		//жмакнуть по ссылке
		private void ClickLink(string link, string param)
		{
			_webClient.Headers.Add("User-Agent", "Mozilla/5.0 (Windows NT 6.1; WOW64; rv:30.0) Gecko/20100101 Firefox/30.0");
			_webClient.Headers[HttpRequestHeader.ContentType] = "application/x-www-form-urlencoded";
			_webClient.Encoding = Encoding.UTF8;
			_html = _webClient.UploadString(Constants.Server + link, param);
		}

		//подключение к серверу
		private void Connect()
		{
			Entery();

			ConnectStatus = "  -  Попытка авторизции";
			string param = string.Format("id5_hf_0=&login={0}&password={1}&%3Asubmit=%D0%92%D1%85%D0%BE%D0%B4", _login, _password);

			try
			{
				ClickLink(_link, param);
			}
			catch (Exception ex)
			{
				ThreadAbort("ОШИБКА" + ex.Message + '\n');
			}

			if (_html.Contains("Поле 'Имя в игре' обязательно для ввода.") || _html.Contains("Неверное имя или пароль"))
			{
				ThreadAbort("ОШИБКА. Неверный логин или пароль.\n");
			}
			ConnectStatus = "  -  Онлайн";

			//фиксируем ссылку на Главную
			_homeLink = Parse(_html, "/home");
			_homeLink = _homeLink.Remove(0, _homeLink.IndexOf('/') + 1);
			_homeLink = _homeLink.Remove(_homeLink.IndexOf('\"') - 1);

			Thread.Sleep(_rnd.Next(1001, 2000));
		}
	}
}