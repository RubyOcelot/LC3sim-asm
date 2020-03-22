using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.IO;
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
using System.Windows.Threading;

namespace LC3_Simulator_WPF
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>

    public class IntTo16bitBinaryConverter : IValueConverter
    {
        object IValueConverter.Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return Convert.ToString((int)value, 2).PadLeft(16, '0');
        }

        object IValueConverter.ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return Convert.ToInt32((string)value);
        }
    }
    public class IntTo4bitHexConverter : IValueConverter
    {
        object IValueConverter.Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return "x"+Convert.ToString((int)value, 16).ToUpper().PadLeft(4, '0');
        }

        object IValueConverter.ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return Convert.ToInt32((string)value);
        }
    }

    enum IconIndex
    {
        none = 0, breakpoint = 1, PC = 2, PC_and_br = 3
    }
    public class IntToImageConverter : IValueConverter
    {
        public List<BitmapImage> iconList = new List<BitmapImage>();

        public IntToImageConverter()
        {
            iconList.Add(new BitmapImage(new Uri("C:\\Users\\14832\\Pictures\\none.png")));
            iconList.Add(new BitmapImage(new Uri("C:\\Users\\14832\\Pictures\\br.png")));
            iconList.Add(new BitmapImage(new Uri("C:\\Users\\14832\\Pictures\\PC.png")));
            iconList.Add(new BitmapImage(new Uri("C:\\Users\\14832\\Pictures\\PC_and_br.png")));
        }
        object IValueConverter.Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return iconList[(int)value];
        }

        object IValueConverter.ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return -1;
        }
    }

    public class StringTo6bitWidthString : IValueConverter
    {
        object IValueConverter.Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return ((string)value).PadRight(6, ' ');
        }

        object IValueConverter.ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return "";
        }
    }

    public class StringTo10bitWidthString : IValueConverter
    {
        object IValueConverter.Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string strValue= (string)value;
            if(strValue.Length<10)
                return strValue.PadRight(10, ' ');
            return (strValue.Remove(8) + "..");
        }

        object IValueConverter.ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return "";
        }
    }
    

    public partial class LC3_Simulator_Window : Window, INotifyPropertyChanged
    {
        public LC3_Simulator_Window()
        {
            InitializeComponent();
            InitializeRegisters();
            InitializeMemory();
            //memoryDisplay.ItemsSource = memoryList;
            //registerDisplay.ItemsSource = registerList;
            InitializeClock();
            InstructionNum.Value = 0;
            LoadOS();
        }

        public class SpaceUnit : INotifyPropertyChanged
        {
            public int address, data, icon;
            public bool hasBreakPoint;
            public string label, instruction;
            public int Address
            {
                get { return address; }
                set
                {
                    address = value;
                    // Call OnPropertyChanged whenever the property is updated
                    OnPropertyChanged("Address");
                }
            }
            public int Data
            {
                get { return data; }
                set
                {
                    data = value;
                    // Call OnPropertyChanged whenever the property is updated
                    OnPropertyChanged("Data");
                }
            }
            public string Label
            {
                get { return label; }
                set
                {
                    label = value;
                    // Call OnPropertyChanged whenever the property is updated
                    OnPropertyChanged("Label");
                }
            }
            public string Instruction
            {
                get { return instruction; }
                set
                {
                    instruction = value;
                    // Call OnPropertyChanged whenever the property is updated
                    OnPropertyChanged("Instruction");
                }
            }
            public int Icon
            {
                get { return icon; }
                set
                {
                    icon = value;
                    // Call OnPropertyChanged whenever the property is updated
                    OnPropertyChanged("Icon");
                }
            }
            public SpaceUnit(int address = 0)
            {
                this.address = address;
                data = 0;
                label = "";
                instruction = "";
                icon = (int)IconIndex.none;
                hasBreakPoint = false;
            }

            public event PropertyChangedEventHandler PropertyChanged;
            protected void OnPropertyChanged(string name)
            {
                PropertyChangedEventHandler handler = PropertyChanged;
                if (handler != null)
                {
                    handler(this, new PropertyChangedEventArgs(name));
                }
            }
        }

        public class RegisterUnit : INotifyPropertyChanged
        {
            public int data, address;
            public string name;
            public int Data
            {
                get { return data; }
                set
                {
                    data = value;
                    // Call OnPropertyChanged whenever the property is updated
                    OnPropertyChanged("Data");
                }
            }
            public string Name
            {
                get { return name; }
                set
                {
                    name = value;
                    // Call OnPropertyChanged whenever the property is updated
                    OnPropertyChanged("Name");
                }
            }
            public RegisterUnit(string name = "", int address = 0)
            {
                this.name = name;
                data = 0;
                this.address = address;
            }

            public event PropertyChangedEventHandler PropertyChanged;
            protected void OnPropertyChanged(string name)
            {
                PropertyChangedEventHandler handler = PropertyChanged;
                if (handler != null)
                {
                    handler(this, new PropertyChangedEventArgs(name));
                }
            }
        }

        public const int MAXREGNUM = 15;
        public ObservableCollection<SpaceUnit> memoryList = new ObservableCollection<SpaceUnit>();
        public ObservableCollection<RegisterUnit> registerList = new ObservableCollection<RegisterUnit>();
        public ObservableCollection<int> jumpList = new ObservableCollection<int>();

        public ObservableCollection<SpaceUnit> MemoryList
        {
            get { return memoryList; }
            set
            {
                memoryList = value;
                OnPropertyChanged("MemoryList");
            }
        }
        public ObservableCollection<RegisterUnit> RegisterList
        {
            get { return registerList; }
            set
            {
                registerList = value;
                OnPropertyChanged("RegisterList");
            }
        }
        public ObservableCollection<int> JumpList
        {
            get { return jumpList; }
            set
            {
                jumpList = value;
                OnPropertyChanged("JumpList");
            }
        }
        public class ObservableInt : INotifyPropertyChanged
        {
            int value;
            public int Value
            {
                get { return value; }
                set
                {
                    this.value = value;
                    OnPropertyChanged("Value");
                }
            }
            public event PropertyChangedEventHandler PropertyChanged;
            protected void OnPropertyChanged(string name)
            {
                PropertyChangedEventHandler handler = PropertyChanged;
                if (handler != null)
                {
                    handler(this, new PropertyChangedEventArgs(name));
                }
            }
        }
        ObservableInt instructionNum=new ObservableInt();
        public ObservableInt InstructionNum
        {
            get { return instructionNum; }
            set
            {
                this.instructionNum = value;
                OnPropertyChanged("InstructionNum");
            }
        }
        int jumpToValue = 0;
        public int JumpToValue
        {
            get { return jumpToValue; }
            set
            {
                this.jumpToValue = value;
                OnPropertyChanged("JumpToValue");
            }
        }
        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string name)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null)
            {
                handler(this, new PropertyChangedEventArgs(name));
            }
        }
        
        enum RegisterIndex
        {
            PC = 8, IR = 9, PSR = 10, KBSR = 11, KBDR = 12, DSR = 13, DDR = 14,MCR=15,SSP=16,USP=17
        };
        public void InitializeRegisters()
        {
            for (int i = 0; i < 8; ++i)
            {
                registerList.Add( new RegisterUnit("R" + i.ToString()));
            }
            registerList.Add(new RegisterUnit("PC"));
            registerList.Add(new RegisterUnit("IR"));
            registerList.Add( new RegisterUnit("PSR"));
            registerList.Add( new RegisterUnit("KBSR",  0xFE00));
            registerList.Add( new RegisterUnit("KBDR",  0xFE02));
            registerList.Add( new RegisterUnit("DSR",   0xFE04));
            registerList.Add( new RegisterUnit("DDR",   0xFE06));
            registerList.Add( new RegisterUnit("MCR",   0xFFFE));//machine control register
            registerList.Add(new RegisterUnit("SSP"));//Saved       xFDFF
            registerList.Add(new RegisterUnit("USP"));          //xFCFF

            registerList[(int)RegisterIndex.PSR].Data = 0x8002;//
            registerList[(int)RegisterIndex.DSR].Data = 0x8000;
            registerList[(int)RegisterIndex.MCR].Data = 0xFFFF;
            registerList[(int)RegisterIndex.SSP].Data = 0xFDFF;
            registerList[(int)RegisterIndex.USP].Data = 0xFCFF;
        }
        public void InitializeMemory()
        {
            for (int i = 0; i < 0x10000; ++i)
            {
                memoryList.Add(new SpaceUnit(i));
                Disasm(i);
            }
            //MessageBox.Show(registerList[(int)RegisterIndex.MCR].address.ToString());
            memoryList[registerList[(int)RegisterIndex.MCR].address].Data = 0xFFFF;
            Disasm(registerList[(int)RegisterIndex.MCR].address);
            MemoryList[registerList[(int)RegisterIndex.DSR].address].Data = 0x8000;
            Disasm(registerList[(int)RegisterIndex.DSR].address);
            memoryDisplay.SelectedIndex = 0x0000;
            ChangePCSelect(0, 0, false);
        }
        public void InitializeClock()
        {
            lc3Clock.Tick += LC3Clock_Tick;
            lc3Clock.Interval = new TimeSpan(1);
        }

        private void ChangePCSelect(int prePos,int nextPos,bool scrollOrNot)
        {
            //MessageBox.Show(prePos.ToString());
            if (prePos < memoryList.Count)
            {
                
                if (memoryList[prePos].Icon == (int)IconIndex.PC_and_br)
                    memoryList[prePos].Icon = (int)IconIndex.breakpoint;
                else
                    memoryList[prePos].Icon = (int)IconIndex.none;
            }
            if (nextPos < memoryList.Count)
            {
                
                if (memoryList[nextPos].Icon == (int)IconIndex.breakpoint)
                    memoryList[nextPos].Icon = (int)IconIndex.PC_and_br;
                else
                    memoryList[nextPos].Icon = (int)IconIndex.PC;
                memoryDisplay.SelectedIndex = nextPos;
                if(scrollOrNot)
                    memoryDisplay.ScrollIntoView(memoryDisplay.SelectedItem);
            }
        }
        private int ChooseSeg(int source,int high,int low)
            //high to 16, low to 0
        {
            return (source % (1 << high)) >> low;
        }
        private int Setseg(int dest,int high,int low,int value)
        {
            int temp1 = (0xffff - (1 << high) + (1 << low));
            int temp2= (dest & temp1);
            int temp3 = temp2 + (value << low);
            return  temp3;
        }
        private int SEXT(int source,int width)
        {
            if ((source & (1 << (width - 1))) > 0)
            {
                return source - (1 << width);
            }
            else
            {
                return source;
            }
        }
        private int GetNZP(int value)
        {
            int NZP;
            if ((value &0x8000)> 0) NZP = 0b100;
            else if (value == 0) NZP = 0b010;
            else NZP = 0b001;
            return NZP;
        }
        private string ToHexStr(int value)
        {
            return "x" + Convert.ToString(value, 16).ToUpper().PadLeft(4, '0');
        }
        private string ToBinStr(int value)
        {
            return Convert.ToString(value, 2).PadLeft(16, '0');
        }
        private void Disasm(int curAddress)
        {
            int curInstruction = memoryList[curAddress].data;
            int PC = curAddress + 1;
            int DR = ChooseSeg(curInstruction, 12, 9);
            int NZP = DR;
            int SR = DR;
            int SR1 = ChooseSeg(curInstruction, 9, 6);
            int BaseR = SR1;
            int SR2 = ChooseSeg(curInstruction, 3, 0);
            int immi5 = SEXT(ChooseSeg(curInstruction, 5, 0), 5);
            int offset9 = SEXT(ChooseSeg(curInstruction, 9, 0), 9);
            int offset6 = SEXT(ChooseSeg(curInstruction, 6, 0), 6);
            string instructionStr="";
            string headStr = "";
            string bodyStr = "";
            //decode & execute
            switch (curInstruction >> 12)//[15:12]
            {
                case 0b0000://BR NOP
                    if (ChooseSeg(curInstruction, 12, 9) == 0)
                    {
                        headStr = "NOP";
                    }
                    else
                    {
                        headStr = "BR";
                        if (ChooseSeg(curInstruction, 12, 11) > 0)
                            headStr += "N";
                        if (ChooseSeg(curInstruction, 11, 10) > 0)
                            headStr += "Z";
                        if (ChooseSeg(curInstruction, 10, 9) > 0)
                            headStr += "P";
                        if (memoryList[(PC + offset9)&0xffff].label == "")//TODO 
                            bodyStr = ToHexStr((PC + offset9)&0xffff);
                        else
                            bodyStr = memoryList[(PC + offset9)&0xffff].label;
                    }
                    break;
                case 0b0001://ADD
                    headStr += "ADD";
                    bodyStr += "R" + DR.ToString() + ", R" + SR1.ToString()+", ";
                    if (ChooseSeg(curInstruction, 6, 5) > 0)
                    {
                        bodyStr+="#"+immi5.ToString();
                    }
                    else
                    {
                        bodyStr += "R"+SR2.ToString();
                    }
                    break;
                case 0b0010://LD
                    headStr += "LD";
                    bodyStr += "R" + DR.ToString() + ", ";
                    if (memoryList[(PC + offset9)&0xffff].label == "")
                        bodyStr += ToHexStr((PC + offset9)&0xffff);
                    else
                        bodyStr += memoryList[(PC + offset9)&0xffff].label;
                    break;
                case 0b0011://ST
                    headStr += "ST";
                    bodyStr += "R" + DR.ToString() + ", ";
                    if (memoryList[(PC + offset9)&0xffff].label == "")
                        bodyStr += ToHexStr((PC + offset9)&0xffff);
                    else
                        bodyStr += memoryList[(PC + offset9)&0xffff].label;
                    break;
                case 0b0100://JSR JSRR
                    if (ChooseSeg(curInstruction, 12, 11) > 0)
                    {
                        headStr += "JSR";
                        int offset11= SEXT(ChooseSeg(curInstruction, 11, 0), 11);

                        if (memoryList[(PC + offset11)&0xffff].label == "")
                            bodyStr = ToHexStr((PC + offset11)&0xffff);
                        else
                            bodyStr = memoryList[(PC + offset11)&0xffff].label;
                    }
                    else
                    {
                        headStr = "JSRR";
                        bodyStr = "R" + BaseR.ToString();
                    }
                    break;
                case 0b0101://AND
                    headStr += "AND";
                    bodyStr += "R" + DR.ToString() + ", R" + SR1.ToString() + ", ";
                    if (ChooseSeg(curInstruction, 6, 5) > 0)
                    {
                        bodyStr += "#" + immi5.ToString();
                    }
                    else
                    {
                        bodyStr += "R" + SR2.ToString();
                    }
                    break;
                case 0b0110://LDR
                    headStr += "LDR";
                    bodyStr += "R" + DR.ToString() + ", ";
                    bodyStr += "#" + offset6.ToString();
                    break;
                case 0b0111://STR
                    headStr += "STR";
                    bodyStr += "R" + DR.ToString() + ", ";
                    bodyStr += "#" + offset6.ToString();
                    break;
                case 0b1000://RTI 
                    headStr = "RTI";
                    break;
                case 0b1001://NOT
                    headStr += "NOT";
                    bodyStr += "R" + DR.ToString() + ", ";
                    bodyStr += "R" + SR1.ToString();
                    break;
                case 0b1010://LDI
                    headStr += "LDI";
                    bodyStr += "R" + DR.ToString() + ", ";
                    if (memoryList[(PC + offset9)&0xffff].label == "")
                        bodyStr += ToHexStr((PC + offset9)&0xffff);
                    else
                        bodyStr += memoryList[(PC + offset9)&0xffff].label;
                    break;
                case 0b1011://STI
                    headStr += "STI";
                    bodyStr += "R" + DR.ToString() + ", ";
                    if (memoryList[(PC + offset9)&0xffff].label == "")
                        bodyStr += ToHexStr((PC + offset9)&0xffff);
                    else
                        bodyStr += memoryList[(PC + offset9)&0xffff].label;
                    break;
                case 0b1100://JMP RET
                    if (BaseR == 7)
                    {
                        headStr = "RET";
                    }
                    else
                    {
                        headStr = "JMP";
                        bodyStr = "R" + BaseR.ToString();
                    }
                    break;
                case 0b1101://reserved TODO
                    headStr = "RESERVED";
                    break;
                case 0b1110://LEA
                    headStr += "LEA";
                    bodyStr += "R" + DR.ToString() + ", ";
                    if (memoryList[(PC + offset9)&0xffff].label == "")
                        bodyStr += ToHexStr((PC + offset9)&0xffff);
                    else
                        bodyStr += memoryList[(PC + offset9) & 0xffff].label;
                    break;
                case 0b1111://TRAP
                    headStr = "TRAP";
                    int trapvect8 = ChooseSeg(curInstruction, 8, 0);
                    switch (trapvect8)
                    {
                        case 0x20:
                            bodyStr = "GETC";
                            break;
                        case 0x21:
                            bodyStr = "OUT";
                            break;
                        case 0x22:
                            bodyStr = "PUTS";
                            break;
                        case 0x23:
                            bodyStr = "IN";
                            break;
                        case 0x24:
                            bodyStr = "PUTSP";
                            break;
                        case 0x25:
                            bodyStr = "HALT";
                            break;
                        default:
                            bodyStr = "x" + Convert.ToString(trapvect8, 16).ToUpper();
                            break;
;                    }
                    break;
            }
            instructionStr = headStr.PadRight(7, ' ') + bodyStr;
            memoryList[curAddress].Instruction=instructionStr;
        }
        private void Interrupt(ref int PSR,ref int PC,int intvect)
        {
            if (ChooseSeg(PSR, 16, 15) > 0)
            {
                registerList[(int)RegisterIndex.USP].Data = registerList[6].Data;
                registerList[6].Data = registerList[(int)RegisterIndex.SSP].Data;
            }
            memoryList[registerList[6].data].Data = PC;
            memoryList[registerList[6].data - 1].Data = PSR;
            registerList[6].Data -= 2;
            PC = memoryList[intvect].Data;
            PSR &= 0x7fff;
            PSR=Setseg(PSR, 3, 0, 0b010);
            if (intvect == 0x180)
            {
                PSR = Setseg(PSR, 11, 8, 4);
            }
        }
        private void SingleStep()
        {
            //fetch
            int PC = registerList[(int)RegisterIndex.PC].Data;
            int PSR = registerList[(int)RegisterIndex.PSR].Data;

            //keyboard Interrupt 
            if (ChooseSeg(memoryList[registerList[(int)RegisterIndex.KBSR].address].Data, 16, 14) == 0b11)
            {
                if (ChooseSeg(PSR, 11, 8) < 4)
                {
                    Interrupt(ref PSR, ref PC, 0x180);
                }
            }

            //finish fetch 
            int curInstruction = memoryList[PC].Data;
            registerList[(int)RegisterIndex.IR].Data = curInstruction;//update IR
            PC++;

            //prepare for execute
            int DR = ChooseSeg(curInstruction, 12, 9);
            int NZP = DR;
            int SR = DR;
            int SR1 = ChooseSeg(curInstruction, 9, 6);
            int BaseR = SR1;
            int SR2 = ChooseSeg(curInstruction, 3, 0);
            int immi5 = SEXT(ChooseSeg(curInstruction, 5, 0), 5);
            int offset9 = SEXT(ChooseSeg(curInstruction, 9, 0), 9);


            //decode & execute
            switch (curInstruction>>12)//[15:12]
            {
                case 0b0000://BR
                    if (( NZP &ChooseSeg(PSR,3,0))>0){
                        PC += offset9;
                        PC &= 0xffff;
                    }
                    break;
                case 0b0001://ADD
                    if (ChooseSeg(curInstruction, 6, 5) > 0)
                    {
                        registerList[DR].Data = registerList[SR1].Data + immi5;
                    }
                    else
                    {
                        registerList[DR].Data = registerList[SR1].Data + registerList[SR2].Data;
                    }
                    registerList[DR].Data &= 0xffff;
                    NZP = GetNZP(registerList[DR].Data);
                    PSR = Setseg(PSR, 3, 0, NZP);
                    break;
                case 0b0010://LD
                    registerList[DR].Data = memoryList[(PC + offset9)&0xffff].Data;
                    NZP = GetNZP(registerList[DR].Data);
                    PSR = Setseg(PSR, 3, 0, NZP);
                    if (((PC + offset9) & 0xffff) == registerList[(int)RegisterIndex.KBDR].address)
                    {
                        InputAuto();
                    }
                    break;
                case 0b0011://ST
                    memoryList[(PC + offset9)&0xffff].Data = registerList[DR].Data;
                    if(((PC + offset9) & 0xffff) == registerList[(int)RegisterIndex.DDR].address){
                        OutputAuto();
                    }
                    break;
                case 0b0100://JSR JSRR
                    registerList[7].Data = PC;
                    if (ChooseSeg(curInstruction, 12, 11) > 0)
                    {
                        PC += SEXT(ChooseSeg(curInstruction, 11, 0), 11);
                    }
                    else
                    {
                        PC += registerList[BaseR].Data;
                    }
                    PC &= 0xffff;
                    break;
                case 0b0101://AND
                    if (ChooseSeg(curInstruction, 6, 5) > 0)
                    {
                        registerList[DR].Data = registerList[SR1].Data & immi5;
                    }
                    else
                    {
                        registerList[DR].Data = registerList[SR1].Data & registerList[SR2].Data;
                    }
                    NZP = GetNZP(registerList[DR].Data);
                    PSR = Setseg(PSR, 3, 0, NZP);
                    break;
                case 0b0110://LDR
                    registerList[DR].Data = memoryList[(registerList[BaseR].Data + SEXT(ChooseSeg(curInstruction, 6, 0), 6)) & 0xffff].Data;
                    NZP = GetNZP(registerList[DR].Data);
                    PSR = Setseg(PSR, 3, 0, NZP);
                    if (((registerList[BaseR].Data + SEXT(ChooseSeg(curInstruction, 6, 0), 6)) & 0xffff) == registerList[(int)RegisterIndex.KBDR].address)
                    {
                        InputAuto();
                    }
                    break;
                case 0b0111://STR 
                    memoryList[(registerList[BaseR].Data + SEXT(ChooseSeg(curInstruction, 6, 0), 6)) & 0xffff].Data = registerList[SR].Data;
                    if (((registerList[BaseR].Data + SEXT(ChooseSeg(curInstruction, 6, 0), 6)) & 0xffff) == registerList[(int)RegisterIndex.DDR].address)
                    {
                        OutputAuto();
                    }
                    break;
                case 0b1000://RTI 
                    if (ChooseSeg(PSR, 16, 15) > 0)
                    {
                        Interrupt(ref PSR, ref PC, 0x100);
                    }
                    else
                    {
                        registerList[6].Data += 2;
                        PSR = memoryList[registerList[6].Data - 1].Data;
                        PC = memoryList[registerList[6].Data].Data;
                        if (ChooseSeg(PSR, 16, 15) > 0)
                        {
                            registerList[(int)RegisterIndex.SSP].Data = registerList[6].Data;
                            registerList[6].Data = registerList[(int)RegisterIndex.USP].Data;
                        }
                    }
                    break;
                case 0b1001://NOT
                    registerList[DR].Data = (~registerList[SR1].Data)&0xffff;
                    NZP = GetNZP(registerList[DR].Data);
                    PSR = Setseg(PSR, 3, 0, NZP);
                    break;
                case 0b1010://LDI
                    registerList[DR].Data = memoryList[memoryList[(PC + offset9)&0xffff].Data].Data;
                    NZP = GetNZP(registerList[DR].Data);
                    PSR = Setseg(PSR, 3, 0, NZP);
                    if ((memoryList[(PC + offset9) & 0xffff].Data) == registerList[(int)RegisterIndex.KBDR].address)
                    {
                        InputAuto();
                    }
                    break;
                case 0b1011://STI
                    memoryList[memoryList[(PC + offset9)&0xffff].Data].Data = registerList[DR].Data;
                    if ((memoryList[(PC + offset9) & 0xffff].Data) == registerList[(int)RegisterIndex.DDR].address)
                    {
                        OutputAuto();
                    }
                    break;
                case 0b1100://JMP RET
                    PC = registerList[SR1].Data;
                    break;
                case 0b1101://reserved 
                    Interrupt(ref PSR, ref PC, 0x101);
                    break;
                case 0b1110://LEA
                    registerList[DR].Data = PC + offset9;
                    NZP = GetNZP(registerList[DR].Data);
                    PSR = Setseg(PSR, 3, 0, NZP);
                    break;
                case 0b1111://TRAP
                    registerList[7].Data = PC;
                    PC = memoryList[ChooseSeg(curInstruction, 8, 0)].Data;
                    break;
            }

            
            

            ChangePCSelect(registerList[(int)RegisterIndex.PC].Data, PC,false);
            registerList[(int)RegisterIndex.PC].Data = PC;
            registerList[(int)RegisterIndex.PSR].Data = PSR;



            
            //update reglist
            registerList[(int)RegisterIndex.KBDR].Data = memoryList[registerList[(int)RegisterIndex.KBDR].address].data;
            registerList[(int)RegisterIndex.KBSR].Data = memoryList[registerList[(int)RegisterIndex.KBSR].address].data;
            registerList[(int)RegisterIndex.DDR].Data = memoryList[registerList[(int)RegisterIndex.DDR].address].data;
            registerList[(int)RegisterIndex.DSR].Data = memoryList[registerList[(int)RegisterIndex.DSR].address].data;
            registerList[(int)RegisterIndex.MCR].Data = memoryList[registerList[(int)RegisterIndex.MCR].address].data;

        }

        int SearchAdd(int l, int r, int newAddr)
        {
            if (l >= r)
            {
                jumpList.Add(newAddr);
                return 0;
            }
            if (l == r - 1)
            {
                if (jumpList[l] < newAddr)
                {
                    jumpList.Insert(l + 1, newAddr);
                    return l + 1;
                }
                else if (jumpList[l] > newAddr)
                {
                    jumpList.Insert(l, newAddr);
                    return l;
                }
                return l;
            }
            int mid = (l + r) / 2;
            if (jumpList[mid] < newAddr)
            {
                return SearchAdd(mid, r, newAddr);
            }
            else if (jumpList[mid] > newAddr)
            {
                return SearchAdd(l, mid, newAddr);
            }
            return mid;
        }
        private int ReturnValidAddress(string input)
        {
            int l = input.Length;
            if (l == 0) return -1;
            if (l > 5) return -1;
            if (input[0] == 'x')
            {
                if (l == 1) return -1;
                for (int i = 1; i < l; ++i)
                {
                    if (!((input[i] >= 'a' && input[i] <= 'f') || (input[i] >= 'A' && input[i] <= 'F') || Char.IsDigit(input[i]))){
                        return -1;
                    }
                }
                return Convert.ToInt32("0"+input,16);
            }
            for(int i = 0; i < l; ++i)
            {
                if (!Char.IsDigit(input[i]))
                    return -1;
            }
            if (Convert.ToInt32(input) > 65535 || Convert.ToInt32(input) < 0)
                return -1;
            return Convert.ToInt32(input);
        }
        private int ReturnValidValue(string input)
        {
            int l = input.Length;
            if (l == 0) return -1;
            if (input[0] == 'x')
            {
                if (l == 1) return -1;
                if (input[1] == '-')
                {
                    if (l > 6) return -1;
                    if (l == 2) return -1;
                    for (int i = 2; i < l; ++i)
                    {
                        if (!((input[i] >= 'a' && input[i] <= 'f') || (input[i] >= 'A' && input[i] <= 'F') || Char.IsDigit(input[i])))
                        {
                            return -1;
                        }
                    }
                    int retValue= Convert.ToInt32("0x" + input.Substring(2), 16);
                    if (retValue > 0x8000) return -1;
                    return (~retValue+1)&0xffff;
                }
                if (l > 5) return -1;
                for (int i = 1; i < l; ++i)
                {
                    if (!((input[i] >= 'a' && input[i] <= 'f') || (input[i] >= 'A' && input[i] <= 'F') || Char.IsDigit(input[i])))
                    {
                        return -1;
                    }
                }
                return Convert.ToInt32("0" + input, 16);
            }
            if (input[0] == '-')
            {
                if (l > 6) return -1;
                if (l == 1) return -1;
                for (int i = 0; i < l; ++i)
                {
                    if (!Char.IsDigit(input[i]))
                        return -1;
                }
                int retValue = Convert.ToInt32(input.Substring(1));
                if (retValue > 0x8000)
                {
                    return -1;
                }
                return (~retValue + 1) & 0xffff;
            }
            if (l > 5) return -1;
            for (int i = 0; i < l; ++i)
            {
                if (!Char.IsDigit(input[i]))
                    return -1;
            }
            if (Convert.ToInt32(input) > 65535 || Convert.ToInt32(input) < 0)
                return -1;
            return Convert.ToInt32(input);
        }
        private void OutputAuto()
        {
            //display on console
            //if (ChooseSeg(memoryList[registerList[(int)RegisterIndex.DSR].address].Data, 16, 15) == 0)//display always idle
            {
                consoleDisplay.Text += (char)(memoryList[registerList[(int)RegisterIndex.DDR].address].Data & 0xff);
                memoryList[registerList[(int)RegisterIndex.DSR].address].Data |= 0x8000;
            }
        }
        private void InputAuto()
        {
            if(ChooseSeg(memoryList[registerList[(int)RegisterIndex.KBSR].address].Data, 16, 15) > 0)
            {
                memoryList[registerList[(int)RegisterIndex.KBSR].address].Data &= 0x7fff;   //clear bit
                if (consoleInputBox.Text != "")
                {
                    consoleInputBox.Text = consoleInputBox.Text.Substring(1);                   //remove input char
                }
            }
        }
        private void InputBox_TextChange(object sender, RoutedEventArgs e)
        {
            if (consoleInputBox.Text == "")
            {
                //fetched all the charactors
                memoryList[registerList[(int)RegisterIndex.KBSR].address].Data &= 0x7fff;
            }
            else { 
                //input from textbox
                if (ChooseSeg(memoryList[registerList[(int)RegisterIndex.KBSR].address].Data, 16, 15) == 0)//lc3 just read kbdr
                {
                    if (consoleInputBox.Text != "")
                    {
                        memoryList[registerList[(int)RegisterIndex.KBSR].address].Data |= 0x8000;                                   //set bit
                        memoryList[registerList[(int)RegisterIndex.KBDR].address].Data = Convert.ToInt32(consoleInputBox.Text[0]);  //set kbsr
                        //consoleInputBox.Text = consoleInputBox.Text.Substring(1);
                    }
                }
            }
        }
        private void LoadOS()
        {
            //Get the path of specified file
            string filePath = "C:\\Users\\14832\\Documents\\study\\CS\\ICS\\assemblier_and_simulater\\lc3os_own.obj";//DEBUG

            if (!File.Exists(filePath))
            {
                MessageBox.Show("Fail to load lc3os file!");
                return;
            }

            byte[] fileBytes = File.ReadAllBytes(filePath);
            if (fileBytes.GetLength(0) % 2 == 1 || fileBytes.GetLength(0) == 0)
            {
                MessageBox.Show("Invalid obj file!");
                return;
            }

            if (filePath.Substring(filePath.Length - 4) == ".obj")
            {
                var symbolFilePath = filePath.Remove(filePath.Length - 4) + ".sym";
                if (File.Exists(symbolFilePath))
                {
                    using (StreamReader file = new StreamReader(symbolFilePath))
                    {
                        string line;
                        int counter = 0;
                        while ((line = file.ReadLine()) != null)
                        {
                            if (counter >= 2)
                            {
                                int i = 0;
                                string labelName = "", address = "";
                                while ((i < line.Length) && (line[i] == '/' || Char.IsWhiteSpace(line[i]))) i++;
                                if (i == line.Length) continue;
                                while ((i < line.Length) && (!Char.IsWhiteSpace(line[i])))
                                {
                                    labelName += line[i++];
                                }
                                while ((i < line.Length) && (line[i] == '/' || Char.IsWhiteSpace(line[i]))) i++;
                                if (i == line.Length) continue;
                                while ((i < line.Length) && (!Char.IsWhiteSpace(line[i])))
                                {
                                    address += line[i++];
                                }
                                int addressValue = ReturnValidAddress("x" + address);
                                if (addressValue != -1)
                                {
                                    MemoryList[addressValue].Label = labelName;
                                }
                            }
                            counter++;
                        }
                    }
                }
            }

            //get origin address TODO
            int startPos = (((int)fileBytes[0]) << 8) + fileBytes[1];
            SearchAdd(0, jumpToBox.Items.Count, startPos);
            ChangePCSelect(registerList[(int)RegisterIndex.PC].data, startPos, true);
            registerList[(int)RegisterIndex.PC].Data = startPos;
            //load program
            for (int i = 2; i < fileBytes.GetLength(0); i += 2)
            {
                int curLine = (((int)fileBytes[i]) << 8) + fileBytes[i + 1];
                memoryList[startPos + i / 2 - 1].Data = curLine;
                Disasm(startPos + i / 2 - 1);
            }
        }

        private void LoadProgramBtn_Click(object sender, RoutedEventArgs e)
        {
            var filePath = string.Empty;
            //UInt16 lineContent = 0;
            OpenFileDialog openFileDialog = new OpenFileDialog();

            string path = Directory.GetCurrentDirectory();

            //openFileDialog.InitialDirectory = path;
            openFileDialog.InitialDirectory = "C:\\Users\\14832\\Documents\\study\\CS\\ICS\\assemblier_and_simulater";//DEBUG

            openFileDialog.Filter = "obj files (*.obj)|*.obj|All files (*.*)|*.*";
            openFileDialog.FilterIndex = 0;
            openFileDialog.RestoreDirectory = true;

            Nullable<bool> result = openFileDialog.ShowDialog();
            // Get the selected file name and display in a TextBox.
            // Load content of file in a TextBlock
            if (result == true)
            {
                //Get the path of specified file
                filePath = openFileDialog.FileName;

                byte[] fileBytes = File.ReadAllBytes(filePath);
                if (fileBytes.GetLength(0) % 2 == 1|| fileBytes.GetLength(0)==0) {
                    MessageBox.Show("Invalid obj file!");
                    return;
                }

                if (filePath.Substring(filePath.Length - 4) == ".obj")
                {
                    var symbolFilePath = filePath.Remove(filePath.Length - 4) + ".sym";
                    if (File.Exists(symbolFilePath))
                    {
                        using (StreamReader file = new StreamReader(symbolFilePath))
                        {
                            string line;
                            int counter = 0;
                            while ((line = file.ReadLine()) != null)
                            {
                                if (counter >= 2)
                                {
                                    int i = 0;
                                    string labelName = "", address = "";
                                    while ((i < line.Length) && (line[i] == '/' || Char.IsWhiteSpace(line[i]))) i++;
                                    if (i == line.Length) continue;
                                    while ((i < line.Length) && (!Char.IsWhiteSpace(line[i])))
                                    {
                                        labelName += line[i++];
                                    }
                                    while ((i < line.Length) && (line[i] == '/' || Char.IsWhiteSpace(line[i]))) i++;
                                    if (i == line.Length) continue;
                                    while ((i < line.Length) && (!Char.IsWhiteSpace(line[i])))
                                    {
                                        address += line[i++];
                                    }
                                    int addressValue = ReturnValidAddress("x" + address);
                                    if (addressValue != -1)
                                    {
                                        MemoryList[addressValue].Label = labelName;
                                    }
                                }
                                counter++;
                            }
                        }
                    }
                }

                //get origin address TODO
                int startPos = (((int)fileBytes[0]) << 8) + fileBytes[1];
                SearchAdd(0, jumpToBox.Items.Count, startPos);
                ChangePCSelect(registerList[(int)RegisterIndex.PC].data, startPos,true);
                registerList[(int)RegisterIndex.PC].Data = startPos;
                JumpToValue = startPos;
                //load program
                for (int i = 2; i < fileBytes.GetLength(0); i += 2)
                {
                    int curLine = (((int)fileBytes[i]) << 8) + fileBytes[i + 1];
                    memoryList[startPos + i / 2 - 1].Data = curLine;
                    Disasm(startPos + i / 2 - 1);
                }

            }
            
        }
        

        DispatcherTimer lc3Clock = new DispatcherTimer();
        //Run
        private void LC3Clock_Tick(object sender, EventArgs e)
        {

            if (ChooseSeg(memoryList[registerList[(int)RegisterIndex.MCR].address].data,16,15)==0||
                        MemoryList[RegisterList[(int)RegisterIndex.PC].data].hasBreakPoint)
            {
                lc3Clock.Stop();
                memoryDisplay.ScrollIntoView(memoryDisplay.SelectedItem);
            }
            else
            {
                InstructionNum.Value++;
                SingleStep();
            }
        }

        private void RunBtn_Click(object sender, RoutedEventArgs e)
        {
            memoryList[registerList[(int)RegisterIndex.MCR].address].Data = 0xFFFF;
            lc3Clock.Start();
        }

        private void StepOverBtn_Click(object sender, RoutedEventArgs e)
        {
            InstructionNum.Value++;
            //instructionNumDispay.Content = instructionNum;
            SingleStep();
            memoryDisplay.ScrollIntoView(memoryDisplay.SelectedItem);

        }

        private void JumpToBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (jumpToBox.SelectedIndex != -1) {
                // memoryDisplay.ScrollIntoView(memoryDisplay.Items[JumpList[jumpToBox.SelectedIndex]]);
                JumpToValue = JumpList[jumpToBox.SelectedIndex];
            }
            else
            {
                int addr = ReturnValidAddress(jumpToBox.Text);
                if (addr != -1)
                {
                    SearchAdd(0, jumpToBox.Items.Count, addr);
                    memoryDisplay.ScrollIntoView(memoryDisplay.Items[addr]);
                }
            }
        }
        private void OnJumpToBoxTextChanged(object sender, RoutedEventArgs e)
        {
            int addr = ReturnValidAddress(jumpToBox.Text);
            if (addr != -1)
            {
                SearchAdd(0, jumpToBox.Items.Count, addr);
                if(addr<memoryDisplay.Items.Count)
                    memoryDisplay.ScrollIntoView(memoryDisplay.Items[addr]);
            }
        }
        private void JumpToBox_LostFocus(object sender, RoutedEventArgs e)
        {/* TODO
            if (jumpToBox.SelectedItem != null)
                return;
            var newAddress = Convert.ToInt32("0"+jumpToBox.Text,16);
            jumpToBox.SelectedItem = jumpToBox.Items[SearchAdd(0,jumpList.Count,newAddress)];*/
        }
        private void StopBtn_Click(object sender, RoutedEventArgs e)
        {
            memoryList[registerList[(int)RegisterIndex.MCR].address].Data &= 0x7FFF;
        }

        private void SetPC_Click(object sender, RoutedEventArgs e)
        {
            ChangePCSelect(registerList[(int)RegisterIndex.PC].data, memoryDisplay.SelectedIndex, true);
            registerList[(int)RegisterIndex.PC].Data = memoryDisplay.SelectedIndex;
        }

        private void AssemblyBtn_Click(object sender, RoutedEventArgs e)
        {
            var filePath = string.Empty;
            string path = Directory.GetCurrentDirectory();
            //string assemblierPath = path+"\\materials\\LC3assemblier.exe";
            string assemblierPath = "C:\\Users\\14832\\Documents\\study\\CS\\ICS\\assemblier_and_simulater\\LC3assemblier.exe";
             OpenFileDialog openFileDialog = new OpenFileDialog();

            //openFileDialog.InitialDirectory = path;
            openFileDialog.InitialDirectory = "C:\\Users\\14832\\Documents\\study\\CS\\ICS\\assemblier_and_simulater";//DEBUG

            openFileDialog.Filter = "asm files (*.asm)|*.asm|bin files (*.bin)|*.bin|hex files (*.hex)|*.hex|All files (*.*)|*.*";
            openFileDialog.FilterIndex = 0;
            openFileDialog.RestoreDirectory = true;

            Nullable<bool> result = openFileDialog.ShowDialog();
            // Get the selected file name and display in a TextBox.
            // Load content of file in a TextBlock
            if (result == true)
            {
                //Get the path of specified file
                filePath = openFileDialog.FileName;
                System.Diagnostics.Process process = new System.Diagnostics.Process();
                System.Diagnostics.ProcessStartInfo startInfo = new System.Diagnostics.ProcessStartInfo();
                startInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden;
                startInfo.FileName = "cmd.exe";
                startInfo.Arguments = "/C " + assemblierPath + " " + filePath + " > asm_message.tmp";
                process.StartInfo = startInfo;
                process.Start();
                process.WaitForExit();
                var messageFilePath = "asm_message.tmp";
                using (StreamReader file = new StreamReader(messageFilePath))
                {
                    string asmMessage = file.ReadToEnd();
                    MessageBox.Show(asmMessage);
                }
            }
        }

        private void ComboBoxItem_MouseClick(object sender, MouseButtonEventArgs e)
        {

        }

        private void ListViewItem_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            DependencyObject obj = (DependencyObject)e.OriginalSource;
            while (obj != null && obj != memoryDisplay)
            {
                
                if (obj is Image )
                {
                    //ListViewItem myItem = (ListViewItem)VisualTreeHelper.GetParent(VisualTreeHelper.GetParent(obj));
                    int brPos = memoryDisplay.SelectedIndex;
                    //TODO
                    switch (MemoryList[brPos].Icon)
                    {
                        case (int)IconIndex.PC_and_br:
                            MemoryList[brPos].Icon = (int)IconIndex.PC;
                            break;
                        case (int)IconIndex.PC:
                            MemoryList[brPos].Icon = (int)IconIndex.PC_and_br;
                            break;
                        case (int)IconIndex.breakpoint:
                            MemoryList[brPos].Icon = (int)IconIndex.none;
                            break;
                        case (int)IconIndex.none:
                            MemoryList[brPos].Icon = (int)IconIndex.breakpoint;
                            break;
                    }

                    MemoryList[brPos].hasBreakPoint = !MemoryList[brPos].hasBreakPoint;
                            
                    break;
                }
                obj = VisualTreeHelper.GetParent(obj);
            }
        }

        private void MemoryDisplay_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }

        private void ConsoleClrbtn_Click(object sender, RoutedEventArgs e)
        {
            consoleDisplay.Text = "";
        }
    }
}
