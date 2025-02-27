using ReactiveUI;
using System.Application.Models;
using System.Application.Services;
using System.Application.UI.Resx;
using System.Properties;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using static System.Application.Services.CloudService.Constants;

namespace System.Application.UI.ViewModels
{
    public partial class
#if __MOBILE__
        LoginOrRegisterPageViewModel
#else
        LoginOrRegisterWindowViewModel
#endif
        : SendSmsUIHelper.IViewModel
    {
        public
#if __MOBILE__
        LoginOrRegisterPageViewModel
#else
        LoginOrRegisterWindowViewModel
#endif
            () : base()
        {
            Title =
#if !__MOBILE__
                ThisAssembly.AssemblyTrademark + " | " +
#endif
                AppResources.LoginAndRegister;

            FastLogin = ReactiveCommand.CreateFromTask<FastLoginChannel>(async channel =>
            {
                await FastLoginOrRegisterAsync(Close, channel);
            });
            ChooseChannel = ReactiveCommand.CreateFromTask<string>(async channel =>
            {
                if (Enum.TryParse<FastLoginChannel>(channel, out var channel_))
                {
                    await FastLoginOrRegisterAsync(Close, channel_);
                }
                else
                {
                    switch (channel)
                    {
                        case nameof(PhoneNumber):
                            await GoToLoginOrRegisterByPhoneNumberAsync();
                            break;
                    }
                }
            });
            SendSms = ReactiveCommand.CreateFromTask(SendSmsAsync);
            Submit = ReactiveCommand.CreateFromTask(SubmitAsync);
            OpenHyperlink = ReactiveCommand.Create<string>(OpenHyperlink_);

            //SteamConnectService.Current.WhenAnyValue(x => x.CurrentSteamUser)
            //    .Subscribe(_ => this.RaisePropertyChanged(nameof(SteamUser)));
        }

        private string? _PhoneNumber;
        public string? PhoneNumber
        {
            get => _PhoneNumber;
            set => this.RaiseAndSetIfChanged(ref _PhoneNumber, value);
        }

        private string? _SmsCode;
        public string? SmsCode
        {
            get => _SmsCode;
            set => this.RaiseAndSetIfChanged(ref _SmsCode, value);
        }

        private int _TimeLimit = SMSInterval;
        public int TimeLimit
        {
            get => _TimeLimit;
            set
            {
                this.RaiseAndSetIfChanged(ref _TimeLimit, value);
                this.RaisePropertyChanged(nameof(IsUnTimeLimit));
            }
        }

        string _BtnSendSmsCodeText = AppResources.User_GetSMSCode;
        public string BtnSendSmsCodeText
        {
            get => _BtnSendSmsCodeText;
            set => this.RaiseAndSetIfChanged(ref _BtnSendSmsCodeText, value);
        }

        public bool IsUnTimeLimit => TimeLimit != SMSInterval;

        public bool SendSmsCodeSuccess { get; set; }

        bool _IsLoading;
        public bool IsLoading
        {
            get => _IsLoading;
            set => this.RaiseAndSetIfChanged(ref _IsLoading, value);
        }

        const short _LoginStateDefault = 2;
        short _LoginState = _LoginStateDefault;
        public short LoginState
        {
            get => _LoginState;
            set
            {
                //this.RaiseAndSetIfChanged(ref _LoginState, value);
                if (_LoginState == value) return;
                _LoginState = value;
                this.RaisePropertyChanged();
#if __MOBILE__
                Title = _LoginState == 1 ? AppResources.User_PhoneLogin : AppResources.LoginAndRegister;
#endif
            }
        }

        //public SteamUser? SteamUser { get; } = SteamConnectService.Current.CurrentSteamUser;

        public ICommand Submit { get; }

        async Task SubmitAsync()
        {
            if (IsLoading || !this.CanSubmit()) return;

            var request = new LoginOrRegisterRequest
            {
                PhoneNumber = PhoneNumber,
                SmsCode = SmsCode
            };
            IsLoading = true;

            var response = await ICloudServiceClient.Instance.Account.LoginOrRegister(request);

            if (response.IsSuccess)
            {
                await SuccessAsync(response.Content!, Close);
                return;
            }

            IsLoading = false;
        }

        static async Task SuccessAsync(LoginOrRegisterResponse rsp, Action? close)
        {
            await UserService.Current.RefreshUserAsync();
            close?.Invoke();
            var msg = AppResources.Success_.Format((rsp?.IsLoginOrRegister ?? false) ? AppResources.User_Login : AppResources.User_Register);
            Toast.Show(msg);
        }

#if !__MOBILE__
        public void ChangeLoginState(short state)
        {
            LoginState = state;
        }
#endif

        public Action? Close { get; set; }

        public Action? TbPhoneNumberFocus { get; set; }

        public Action? TbSmsCodeFocus { get; set; }

        public CancellationTokenSource? CTS { get; set; }

        public ICommand SendSms { get; }

        async Task SendSmsAsync()
        {
            if (this.TimeStart())
            {
                var request = new SendSmsRequest
                {
                    PhoneNumber = PhoneNumber,
                    Type = SmsCodeType.LoginOrRegister,
                };

#if DEBUG
                var response =
#endif
                await this.SendSms(request);
            }
        }

        public ICommand OpenHyperlink { get; }

        void OpenHyperlink_(string parameter)
        {
            if (string.IsNullOrWhiteSpace(parameter)) return;
            switch (parameter)
            {
                case "Agreement":
                    parameter = "https://steampp.net/AgreementBox";
                    break;
                case "Privacy":
                    parameter = "https://steampp.net/PrivacyBox";
                    break;
            }
#if __MOBILE__
            BrowserOpen(parameter);
#else
            if (AppHelper.IsSystemWebViewAvailable)
            {
                IShowWindowService.Instance.Show(CustomWindow.WebView3, new WebView3WindowViewModel
                {
                    Url = parameter,
                }, resizeMode: ResizeModeCompat.NoResize);
            }
            else
            {
                BrowserOpen(parameter);
            }
#endif
        }

        [Obsolete("use ChooseChannel")]
        public ICommand FastLogin { get; }

        [Obsolete("use Channels")]
        public FastLoginChannel[] FastLoginChannels { get; } = new[] {
            FastLoginChannel.Steam,
            FastLoginChannel.Xbox,
            //FastLoginChannel.Apple,
            //FastLoginChannel.QQ,
        };

        public string[] Channels { get; } = new[]
        {
            nameof(FastLoginChannel.Steam),
            nameof(FastLoginChannel.Xbox),
#if DEBUG
            nameof(FastLoginChannel.Apple),
            nameof(FastLoginChannel.QQ),
#endif
            nameof(PhoneNumber),
        };

        public ICommand ChooseChannel { get; }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                CTS?.Cancel();
            }
            base.Dispose(disposing);
        }
    }
}