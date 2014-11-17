using System;
using System.Drawing;
using System.Threading;
using System.Windows.Forms;

namespace Nebo.Mobi.Bot
{
	public partial class Form1 : Form
	{
		private readonly Manager _manager;
		private readonly System.Windows.Forms.Timer _botTimer; //таймер запуска прохода бота
		private Thread _bot; //переменная потока бота

		public Form1()
		{
			InitializeComponent();
			_manager = new Manager(_botTimer);
			FormInitialization();

			tbPass.UseSystemPasswordChar = true;

			_botTimer = new System.Windows.Forms.Timer();
			_botTimer.Tick += bot_timer_Tick;

			ref_timer.Interval = 1000;
			_botTimer.Enabled = false;
			ref_timer.Enabled = false;

		}

		/// <summary>
		/// установка значений ширины\высоты для формы
		/// </summary>
		private void FormInitialization()
		{
			Size = new Size((int) (0.5*Screen.PrimaryScreen.Bounds.Width), (int) (0.5*Screen.PrimaryScreen.Bounds.Height));

			lUserInfo.Location = new Point((int) (0.02*Size.Width), (int) (0.02*Size.Height));
			lDiapazon.Location = new Point(lUserInfo.Location.X + lUserInfo.Size.Width + (int) (0.3*Size.Height),
				lUserInfo.Location.Y);

			lLogin.Location = new Point(lUserInfo.Location.X,
				lUserInfo.Location.Y + lUserInfo.Size.Height + (int) (0.01*Size.Height));
			tbLogin.Location = new Point(lLogin.Location.X + lLogin.Size.Width + (int) (0.005*Size.Width), lLogin.Location.Y - 2);
			tbLogin.Size = new Size((int) (0.2*Size.Width), tbLogin.Size.Height);

			lPass.Location = new Point(lLogin.Location.X, lLogin.Location.Y + lLogin.Size.Height + (int) (0.02*Size.Height));
			tbPass.Location = new Point(tbLogin.Location.X, lPass.Location.Y - 2);
			tbPass.Size = new Size((int) (0.2*Size.Width), tbPass.Size.Height);

			lMinTime.Location = new Point(lDiapazon.Location.X, lLogin.Location.Y);
			tbMinTime.Location = new Point(lMinTime.Location.X + lMinTime.Size.Width + (int) (0.005*Size.Width),
				lMinTime.Location.Y - 2);
			tbMinTime.Size = new Size((int) (0.05*Size.Width), tbMinTime.Size.Height);

			cbDoNotPut.Location = new Point(lDiapazon.Location.X + lDiapazon.Size.Width + (int) (0.01*Size.Width),
				tbMinTime.Location.Y);

			lMaxTime.Location = new Point(lDiapazon.Location.X,
				lMinTime.Location.Y + lMinTime.Size.Height + (int) (0.02*Size.Height));
			tbMaxTime.Location = new Point(tbMinTime.Location.X, lMaxTime.Location.Y - 2);
			tbMaxTime.Size = new Size((int) (0.05*Size.Width), tbMinTime.Size.Height);

			bStart.Location = new Point(lLogin.Location.X, (int) (0.15*Size.Height));
			bStop.Location = new Point(tbLogin.Location.X + tbLogin.Size.Width - bStop.Size.Width, bStart.Location.Y);
			bStop.Enabled = false;

			lLOG.Location = new Point(lLogin.Location.X, (int) (0.3*Size.Height));

			LOGBox.Location = new Point(lLogin.Location.X, lLOG.Location.Y + lLOG.Size.Height + (int) (0.01*Size.Height));
			LOGBox.Size = new Size(Size.Width - 3*LOGBox.Location.X, (int) (0.5*Size.Height));

			lCopyright.Text = @"Exclusive by Mr.President  ©  2014." + @"  ver. 1.4";
			lCopyright.Location = new Point(lLOG.Location.X + LOGBox.Size.Width - lCopyright.Size.Width, (int) (Size.Height*0.88));
		}


		public static string Name = "Небоскребы. Бот"; //имя окна

		/// <summary>
		/// обновление содержания формы
		/// </summary>
		private void UpdForm()
		{
			if (!_bot.IsAlive)
			{
				bStart.Enabled = true;
				bStop.Enabled = false;
			}
			Text = Name + _manager.ConnectStatus + _manager.ActionStatus;
			if (_manager.CommutationStr != "")
			{
				LOGBox.Text += _manager.CommutationStr;
				LOGBox.SelectionStart = LOGBox.TextLength;
				LOGBox.ScrollToCaret();
				_manager.CommutationStr = "";
			}
			if (Text.Contains("Стоп"))
			{
				_botTimer.Start();
				_manager.ConnectStatus = "";
			}

			if (_manager.Timeleft > 0)
			{
				_manager.ConnectStatus = string.Format("   Жду   {0}мин : {1:d2}сек\n\n", _manager.Timeleft/60,
					_manager.Timeleft - (_manager.Timeleft/60*60));
				_manager.Timeleft--;
			}
		}


		/// <summary>
		/// обработчик кнопки Старт
		/// </summary>
		private void bStart_Click(object sender, EventArgs e)
		{
			StartBot();
		}

		/// <summary>
		/// Стартует бота
		/// </summary>
		private void StartBot()
		{
			bStart.Enabled = false;
			bStop.Enabled = true;
			_bot =
				new Thread(
					obj =>
						_manager.StartBot(cbDoNotPut.Checked, Convert.ToInt32(tbMinTime.Text)*60000, Convert.ToInt32(tbMaxTime.Text)*60000,
							tbLogin.Text.Replace(' ', '+'), tbPass.Text));
			ref_timer.Start();
			_bot.Start();
		}


		/// <summary>
		/// остановка бота и очистка синхрострок
		/// </summary>
		private void bStop_Click(object sender, EventArgs e)
		{
			_bot.Abort();
			_manager.ConnectStatus = "";
			_manager.ActionStatus = "";
			_botTimer.Stop();
			ref_timer.Stop();
			bStart.Enabled = true;
			bStop.Enabled = false;
			_manager.Timeleft = 0;
			UpdForm();
		}

		//событие таймера
		private void bot_timer_Tick(object sender, EventArgs e)
		{
			_botTimer.Stop();
			StartBot();
		}

		//обработчик таймера обновления формы и статусов
		private void ref_timer_Tick(object sender, EventArgs e)
		{
			UpdForm();
		}

		private void Form1_FormClosed(object sender, FormClosedEventArgs e)
		{
			try
			{
				_bot.Abort();
			}
			catch (Exception)
			{
			}
		}
	}
}