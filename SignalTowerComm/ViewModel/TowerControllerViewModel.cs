using GalaSoft.MvvmLight.Command;
using SignalTowerComm.ViewModel.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Threading;
using System.Collections;
using System.IO;

namespace SignalTowerComm.ViewModel
{
    public class TowerControllerViewModel : CustomViewModelBase
    {
        enum C
        {
            R,
            G,
            Y,
            RGY,
            Buzzer
        }

        Class.TowerControllerDll cTowerControllerDll;

        #region 프로퍼티
        public string StrCommandLine {
            get { return _strCommandLine; }
            set { _strCommandLine = value; RaisePropertyChanged("StrCommandLine");  } }
        private string _strCommandLine="";

        //버튼 이벤트 류
        public string StrRedCommand
        {
            get { return _strRedCommand; }
            set { _strRedCommand = value; RaisePropertyChanged("StrRedCommand"); }
        }
        private string _strRedCommand = "Red ON";

        public bool BRedOnOff
        {
            get { return _bRedOnOff; }
            set { if (BRedOnOff) { StrRedCommand = "Red ON"; } else { StrRedCommand = "Red OFF"; } _bRedOnOff = value;  RaisePropertyChanged("BRedOnOff"); }
        }
        public bool _bRedOnOff = false;

        public string StrYellowCommand
        {
            get { return _strYellowCommand; }
            set { _strYellowCommand = value; RaisePropertyChanged("StrYellowCommand"); }
        }
        private string _strYellowCommand = "Yellow ON";

        public bool BYellowOnOff
        {
            get { return _bYellowOnOff; }
            set { if (_bYellowOnOff) { StrYellowCommand = "Yellow ON"; } else { StrYellowCommand = "Yellow OFF"; } _bYellowOnOff = value; RaisePropertyChanged("BYellowOnOff"); }
        }
        public bool _bYellowOnOff = false;

        public string StrGreenCommand
        {
            get { return _strGreenCommand; }
            set { _strGreenCommand = value; RaisePropertyChanged("StrGreenCommand"); }
        }
        private string _strGreenCommand = "Green ON";

        public bool BGreenOnOff
        {
            get { return _bGreenOnOff; }
            set { if (_bGreenOnOff) { StrGreenCommand = "Green ON"; } else { StrGreenCommand = "Green OFF"; } _bGreenOnOff = value; RaisePropertyChanged("BGreenOnOff"); }
        }
        public bool _bGreenOnOff = false;

        public string StrBuzzerCommand
        {
            get { return _strBuzzerCommand; }
            set { _strBuzzerCommand = value; RaisePropertyChanged("StrBuzzerCommand"); }
        }
        private string _strBuzzerCommand = "Buzzer ON";

        public bool BBuzzerOnOff
        {
            get { return _bBuzzerOnOff; }
            set { if (_bBuzzerOnOff) { StrBuzzerCommand = "Buzzer ON"; } else { StrBuzzerCommand = "Buzzer OFF"; } _bBuzzerOnOff = value; RaisePropertyChanged("BBuzzerOnOff"); }
        }
        public bool _bBuzzerOnOff = false;
 
        public string StrLogLine
        {
            get { return _strLogLine; }
            set { _strLogLine = value + Environment.NewLine + _strLogLine; RaisePropertyChanged("StrLogLine"); }
        }
        private string _strLogLine = "";
        private bool isStarted = false;
        private volatile bool stopped = false;
        private volatile bool running = true;
        private Thread mThread;
        private Thread ioThread;
        private bool alarmOccur = false;

        #endregion

        #region 커맨드
        public RelayCommand<object> SendCommand { get; private set; }
        public RelayCommand<object> ResetCommand { get; private set; }
        public RelayCommand<object> RedCommand { get; private set; }
        public RelayCommand<object> YellowCommand { get; private set; }
        public RelayCommand<object> GreenCommand { get; private set; }
        public RelayCommand<object> BuzzerCommand { get; private set; }
        public RelayCommand<object> StartCommand { get; private set; }
        public RelayCommand<object> StopCommand { get; private set; }
        #endregion

        #region 초기화
        public TowerControllerViewModel()
        {
            InitData();
            InitCommand();
        }

        private void InitData()
        {
            cTowerControllerDll = new Class.TowerControllerDll();
            cTowerControllerDll.readUsbConnectList();
        }

        private void InitCommand()
        {
            StartCommand = new RelayCommand<object>((param) => OnStartCommand(param));
            SendCommand = new RelayCommand<object>((param) => OnSendCommand(param));
            ResetCommand = new RelayCommand<object>((param) => OnResetCommand(param));
            RedCommand = new RelayCommand<object>((param) => OnRedCommand(param));
            YellowCommand = new RelayCommand<object>((param) => OnYellowCommand(param));
            GreenCommand = new RelayCommand<object>((param) => OnGreenCommand(param));
            BuzzerCommand = new RelayCommand<object>((param) => OnBuzzerCommand(param));
            StopCommand = new RelayCommand<object>((param) => OnStopCommand(param));
        }



        #endregion

        #region 이벤트
        private void OnStopCommand(object param)
        {
            Console.WriteLine("Stop Clicked!!!");
            if (isStarted)
            {
                stopThread();
                do
                {
                    Thread.Sleep(2000);
                } while (!stopped);
                isStarted = false;
                command(C.R, false);
                command(C.Buzzer, false);
                Console.WriteLine("Stopped...");
                Console.WriteLine("Start");
                StrLogLine = "Device Stopped";
            }
        }

        private void OnStartCommand(object param)
        {
            if (!isStarted) // Turn On
            {
                isStarted = true;
                running = true;
                mThread = new Thread(new ThreadStart(startThread));
                mThread.Start();
                ioThread = new Thread(new ThreadStart(ioThreadControl));
                ioThread.Start();
                StrLogLine = "Device Started";
                Console.WriteLine("Processing...");
                Console.WriteLine("Stop");
            }
            else // Turn Off 
            {
                stopThread();
                do
                {
                    Thread.Sleep(2000);
                } while (!stopped);
                isStarted = false;
                command(C.R, false);
                command(C.Buzzer, false);
                Console.WriteLine("Stopped...");
                Console.WriteLine("Start");
                StrLogLine = "Device Stopped";
            }
        }

        private void startThread()
        {
            Console.WriteLine("This is worker thread. ThreadID: {0}", Thread.CurrentThread.ManagedThreadId);
            stopped = false;
            while (running)
            {
                ArrayList readlines = new ArrayList();
                ArrayList newList = new ArrayList();
                String file = "C:\\alarm_data.txt";
                String line;
                try
                {
                    StreamReader sr = new StreamReader(file);
                    line = sr.ReadLine();//Read the first line of text                    
                    while (line != null) //Continue to read until you reach end of file
                    {
                        readlines.Add(line);
                        line = sr.ReadLine(); //Read the next line                        
                    }
                    sr.Close(); //close the file
                    bool textUpdated = false;
                    for (int i = 0; i < readlines.Count; i++)
                    {
                        string s = (string)readlines[i]; //Console.WriteLine(s);
                        if (!s.StartsWith("X "))
                        {
                            //process alarm...
                            alarmOccur = true;
                            s = "X " + s;
                            //MessageBox.Show("Alert : " + s);
                            textUpdated = true;
                        }
                        newList.Add(s);
                    }
                    if (textUpdated)
                    {
                        StreamWriter sw = new StreamWriter(file, false);
                        if(newList.Count >= 50)
                        {
                            sw.WriteLine((string)newList[0]);
                        }
                        else
                        {
                            for (int i = 0; i < newList.Count; i++)
                                sw.WriteLine((string)newList[i]);
                        }
                        sw.Close();
                    }
                    //Console.ReadLine();
                }
                catch (Exception e)
                {
                    Console.WriteLine("Exception: " + e.Message);
                }
                finally
                {
                    Console.WriteLine("Executing finally block.");
                }
                Thread.Sleep(1000);
            }
            Console.WriteLine("This is worker thread stoped. ThreadID: {0}", Thread.CurrentThread.ManagedThreadId);
            stopped = true;
        }

        private void ioThreadControl()
        {
            while (running)
            {
                if (alarmOccur)
                {
                    command(C.R, true);
                    command(C.Buzzer, true);
                    Thread.Sleep(7000);
                    command(C.R, false);
                    command(C.Buzzer, false);
                    alarmOccur = false;
                }
            }
        }


        public void stopThread()
        {
            if (mThread != null)
            {
                running = false;
            }
        }


        private void command(C c, bool status)
        {
            if(c == C.R)
            {
                byte[] bCommandArr = new byte[8];

                for (int iLoofCount = 0; iLoofCount < 8; iLoofCount++)
                    bCommandArr[iLoofCount] = byte.Parse("9");

                bCommandArr[0] = bCommandArr[1] = 0;

                if (!status)
                    bCommandArr[2] = byte.Parse("0");
                else
                    bCommandArr[2] = byte.Parse("1");

                if (cTowerControllerDll.sendCommandSignalTower(bCommandArr))
                    StrLogLine = "Red Toggle OK";
                else
                    StrLogLine = "Send Fail";
                return;
            }
            if(c == C.Buzzer)
            {
                byte[] bCommandArr = new byte[8];

                for (int iLoofCount = 0; iLoofCount < 8; iLoofCount++)
                {
                    bCommandArr[iLoofCount] = byte.Parse("9");
                }

                bCommandArr[0] = bCommandArr[1] = 0;

                if (!status)
                    bCommandArr[7] = byte.Parse("0");
                else
                    bCommandArr[7] = byte.Parse("1");


                if (cTowerControllerDll.sendCommandSignalTower(bCommandArr))
                {
                    StrLogLine = "Buzzer Toggle OK";
                }
                else
                {
                    StrLogLine = "Send Fail";
                }
                return;
            }
        }


        private void OnSendCommand(object param)
        {
            byte[] bCommandArr = new byte[8];
            //!!
            if (StrCommandLine.Length != 8)
            {
                MessageBox.Show("8개로 구성된 커맨드가 필요합니다");
            }
            else
            {
                for (int iLoofCount = 0; iLoofCount < 8; iLoofCount++)
                {
                    bCommandArr[iLoofCount] = byte.Parse(StrCommandLine.Substring(iLoofCount, 1));
                }

                if (cTowerControllerDll.sendCommandSignalTower(bCommandArr))
                {
                    StrLogLine = "SendCommand OK";
                }
                else
                {
                    StrLogLine = "SendCommand Fail";
                }
            }
        }

        private void OnResetCommand(object param)
        {
            byte[] bCommandArr = new byte[8];

            for (int iLoofCount = 0; iLoofCount < 8; iLoofCount++)
            {
                bCommandArr[iLoofCount] = byte.Parse("0");
            }

            if (cTowerControllerDll.sendCommandSignalTower(bCommandArr))
            {
                StrLogLine = "SendReset OK";
                if (BRedOnOff) { BRedOnOff = false; } else {  }
                if (BYellowOnOff) { BYellowOnOff = false; } else { }
                if (BGreenOnOff) { BGreenOnOff = false; } else { }
                if (BBuzzerOnOff) { BBuzzerOnOff = false; } else { }
            }
            else
            {
                StrLogLine = "SendReset Fail";
            }
        }

        private void OnRedCommand(object param)
        {
            byte[] bCommandArr = new byte[8];

            for (int iLoofCount = 0; iLoofCount < 8; iLoofCount++)
            {
                bCommandArr[iLoofCount] = byte.Parse("9");
            }

            bCommandArr[0] = bCommandArr[1] = 0;

            if(BRedOnOff)
            {
                bCommandArr[2] = byte.Parse("0");
                BRedOnOff = false;
            }
            else
            {
                bCommandArr[2] = byte.Parse("1");
                BRedOnOff = true;
            }
            

            if (cTowerControllerDll.sendCommandSignalTower(bCommandArr))
            {
                StrLogLine = "Red Toggle OK";
                
            }
            else
            {
                StrLogLine = "Send Fail";
            }
        }

        private void OnYellowCommand(object param)
        {
            byte[] bCommandArr = new byte[8];

            for (int iLoofCount = 0; iLoofCount < 8; iLoofCount++)
            {
                bCommandArr[iLoofCount] = byte.Parse("9");
            }

            bCommandArr[0] = bCommandArr[1] = 0;

            if (BYellowOnOff)
            {
                bCommandArr[3] = byte.Parse("0");
                BYellowOnOff = false;
            }
            else
            {
                bCommandArr[3] = byte.Parse("1");
                BYellowOnOff = true;
            }


            if (cTowerControllerDll.sendCommandSignalTower(bCommandArr))
            {
                StrLogLine = "Yellow Toggle OK";

            }
            else
            {
                StrLogLine = "Send Fail";
            }
        }

        private void OnGreenCommand(object param)
        {
            byte[] bCommandArr = new byte[8];

            for (int iLoofCount = 0; iLoofCount < 8; iLoofCount++)
            {
                bCommandArr[iLoofCount] = byte.Parse("9");
            }

            bCommandArr[0] = bCommandArr[1] = 0;

            if (BGreenOnOff)
            {
                bCommandArr[4] = byte.Parse("0");
                BGreenOnOff = false;
            }
            else
            {
                bCommandArr[4] = byte.Parse("1");
                BGreenOnOff = true;
            }


            if (cTowerControllerDll.sendCommandSignalTower(bCommandArr))
            {
                StrLogLine = "Green Toggle OK";

            }
            else
            {
                StrLogLine = "Send Fail";
            }
        }

        private void OnBuzzerCommand(object param)
        {
            byte[] bCommandArr = new byte[8];

            for (int iLoofCount = 0; iLoofCount < 8; iLoofCount++)
            {
                bCommandArr[iLoofCount] = byte.Parse("9");
            }

            bCommandArr[0] = bCommandArr[1] = 0;

            if (BBuzzerOnOff)
            {
                bCommandArr[7] = byte.Parse("0");
                BBuzzerOnOff = false;
            }
            else
            {
                bCommandArr[7] = byte.Parse("1");
                BBuzzerOnOff = true;
            }


            if (cTowerControllerDll.sendCommandSignalTower(bCommandArr))
            {
                StrLogLine = "Buzzer Toggle OK";

            }
            else
            {
                StrLogLine = "Send Fail";
            }
        }
        #endregion
    }
}
