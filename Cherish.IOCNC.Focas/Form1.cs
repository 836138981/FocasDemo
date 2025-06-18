using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml.Serialization;
using static Focas1;

namespace Cherish.IOCNC.Focas
{
    public partial class Form1 : Form
    {
        private short _ret = 0;
        private ushort _flibhndl;
        private bool _isConnect=false;
        public Form1()
        {
            InitializeComponent();

            this.cmb_TimerType.SelectedIndex= 0;
        }

        private void 说明ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            DescriptionForm frm = new DescriptionForm();
            frm.ShowDialog();
        }
        #region 连接
        private void btn_Connect_Click(object sender, EventArgs e)
        {
            var prot = ushort.Parse(this.txt_Port.Text.Trim());
            _ret = Focas1.cnc_allclibhndl3(this.txt_IP.Text.Trim(), prot, 10, out _flibhndl);
            if(_ret == 0)
            {
                Print("Connect Success");

                _isConnect =true;

              
            }
            else
            {
                Print("Connect Fail");
                _isConnect = false;
            }
        }

        private void btn_DisConnect_Click(object sender, EventArgs e)
        {
            _ret = Focas1.cnc_freelibhndl(_flibhndl);
            if (_ret == 0)
            {
                Print("DisConnect Success");
                _isConnect = false;
            }
            else
            {
                Print("DisConnect Fail");
                _isConnect = false;
            }
        }
        #endregion

        private void Print(string message)
        {
            this.Invoke(new Action(() =>
            {
            this.richTextBox1.AppendText($"{message}{Environment.NewLine}");
            }));
          
        }

        #region 报警信息乱码问题
        private void btn_AlarmMsg2_Click(object sender, EventArgs e)
        {
            //问题分析
            //报警信息默认按照windows系统默认的编码方式转化成了字符串
            //当前 系统默认是按照简体中文 GB2312编码的方式转化成了字符串
            //当机床默认的语言和电脑默认的语言编码方式不一样就会出现乱码问题

            //
            //解决方案1 将字符串改成byte[]  用特定的编码方式来获取字符串
            //解决方案2 将系统的默认语言设置成和机床语言一致   繁体中文机床使用的编码方式是Shift-JIS 电脑语言繁体中文（中国台湾）默认编码方式是BIG5

            //最终采用方案1

            //通过参数3281读取机床的语言
            Focas1.IODBPSD_1 psd_1 = new Focas1.IODBPSD_1();
            short length = 4 + 1;
            _ret = Focas1.cnc_rdparam(_flibhndl, 3281, 0, length, psd_1);
            if(_ret == 0)
            {
                Print($"cnc_rdparam success,{psd_1.idata}");
            }
            else
            {
                Print($"cnc_rdparam fail,ret:{_ret}");
                return; 
            }
            Encoding encoding = Encoding.Default;
            switch (psd_1.idata)
            {
                //英语
                case 0:
                    encoding=Encoding.ASCII;
                    break;
                case 1://日文
                case 4://繁体中文
                    encoding = Encoding.GetEncoding("Shift-JIS");
                    break;
                case 15://简体中文
                    encoding = Encoding.GetEncoding("GB2312");
                    break;
                default:
                    break;
            }
            Focas1.ODBALMMSG2 allAlarms = new Focas1.ODBALMMSG2();
            short numberOfAlarms = 1;
            _ret = Focas1.cnc_rdalmmsg2(_flibhndl, -1, ref numberOfAlarms, allAlarms);
            if (_ret == 0) 
            {
                string msg = encoding.GetString(allAlarms.msg1.alm_msg.Take(allAlarms.msg1.msg_len).ToArray());
                Print(msg);
            }
            else
            {
                Print($"cnc_rdalmmsg2 fail,ret:{_ret}");
            }
        }
        #endregion
        /// <summary>
        /// 读取诊断号
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btn_RdDiagnoss_Click(object sender, EventArgs e)
        {
            short s_number = (short)this.num_DiagNum.Value;
            ushort read_no = 1;
            ODBDIAGIF oDBDIAGIF = new ODBDIAGIF();
            _ret = Focas1.cnc_rddiaginfo(_flibhndl, s_number, read_no, oDBDIAGIF);
            if (_ret == 0)
            {
                Print($"diag_no:{oDBDIAGIF.info.info1.diag_no},diag_type:{oDBDIAGIF.info.info1.diag_type}");
                short type = oDBDIAGIF.info.info1.diag_type;
                var diag_no = oDBDIAGIF.info.info1.diag_no;
                int dateType = (type >> 0) & 0b0000_0011;
                bool withAxis = ((type >> 2) & 0b0000_0001) == 1;
                //int typeAttr = ((type >> 3) & 0b0000_0001);//是不是 也不是

                int sign = ((type >> 7) & 0b0000_0001);//0是无符号 1是有符号  有点问题
                int spindleParameter = ((type >> 8) & 0b0000_0001);
                int isReal = ((type >> 12) & 0b0000_0001);

                short axis = (short)this.num_AxisNO.Value;
                short length = 4 + 8 * 1;

      
                switch (dateType)
                {
                     
                    case 0:
                        Print($"{diag_no} is bit type，WithAxis:{withAxis}，Sign:{sign == 1}，SpindleParameter:{spindleParameter == 1}");
                         length = 4 + 1 * 1;
                        ODBDGN_1 oDBDGN0 = new ODBDGN_1();
                        _ret = Focas1.cnc_diagnoss(_flibhndl, s_number, axis, length, oDBDGN0);
                        if (_ret == 0)
                        {
                           int value =(oDBDGN0.cdata >> (byte)this.num_BitNum.Value)&1;
                            Print($"Diag_No:{oDBDGN0.datano},Value:{value}");
                        }
                        else
                        {
                            Print($"cnc_diagnoss fail,ret:{_ret}");
                        }
                        break;
                    case 1:
                        Print($"{diag_no} is byte type，WithAxis:{withAxis}，Sign:{sign == 1}，SpindleParameter:{spindleParameter == 1}");
                        length = 4 + 1 * 1;
                        ODBDGN_1 oDBDGN1 = new ODBDGN_1();
                        _ret = Focas1.cnc_diagnoss(_flibhndl, s_number, axis, length, oDBDGN1);
                        if (_ret == 0)
                        {
                            Print($"Diag_No:{oDBDGN1.datano},Value:{oDBDGN1.cdata}");
                        }
                        else
                        {
                            Print($"cnc_diagnoss fail,ret:{_ret}");
                        }
                        break;
                    case 2:
                        Print($"{diag_no} is word type，WithAxis:{withAxis}，Sign:{sign == 1}，SpindleParameter:{spindleParameter == 1}");
                        length = 4 + 2 * 1;
                        ODBDGN_1 oDBDGN2 = new ODBDGN_1();
                        _ret = Focas1.cnc_diagnoss(_flibhndl, s_number, axis, length, oDBDGN2);
                        if (_ret == 0)
                        {
                            Print($"Diag_No:{oDBDGN2.datano},Value:{oDBDGN2.idata}");
                        }
                        else
                        {
                            Print($"cnc_diagnoss fail,ret:{_ret}");
                        }
                        break;
                    case 3:
                        if (isReal == 1)
                        {
                            Print($"{diag_no} is real,WithAxis:{withAxis}，Sign:{sign == 1}，SpindleParameter:{spindleParameter == 1} ");
                            length = 4 + 8 * 1;
                            ODBDGN_2 oDBDGN = new ODBDGN_2();
                            _ret = Focas1.cnc_diagnoss(_flibhndl, s_number, axis, length, oDBDGN);
                            if (_ret == 0)
                            {
                                var value = Math.Round(oDBDGN.rdata.dgn_val * Math.Pow(0.1, oDBDGN.rdata.dec_val), oDBDGN.rdata.dec_val);
                                Print($"Diag_No:{oDBDGN.datano},Value:{value}");
                            }
                            else
                            {
                                Print($"cnc_diagnoss fail,ret:{_ret}");
                            }
                        }
                        else
                        {
                            Print($"{diag_no} is 2-word,WithAxis:{withAxis}，Sign:{sign == 1}，SpindleParameter:{spindleParameter == 1} ");
                            length = 4 + 4 * 1;
                            ODBDGN_1 oDBDGN3 = new ODBDGN_1();
                            _ret = Focas1.cnc_diagnoss(_flibhndl, s_number, axis, length, oDBDGN3);
                            if (_ret == 0)
                            {
                                Print($"Diag_No:{oDBDGN3.datano},Value:{oDBDGN3.idata}");
                            }
                            else
                            {
                                Print($"cnc_diagnoss fail,ret:{_ret}");
                            }

                        }

                        break;
                    default:
                        break;
                }
            }
            else
            {
                Print($"cnc_rddiaginfo fail,ret:{_ret}");
            }
          
        }
        private void btn_Clear_Click(object sender, EventArgs e)
        {
            this.richTextBox1.Clear();
        }


        private void button1_Click(object sender, EventArgs e)
        {
            short number =(short)this.num_MarcoNum.Value;
            short  length =10;
            Focas1.ODBM oDBM = new Focas1.ODBM();
            _ret=Focas1.cnc_rdmacro(_flibhndl, number, length, oDBM);
            if (_ret == 0) 
            {
                double value = Math.Round( oDBM.mcr_val * Math.Pow(10, -oDBM.dec_val), oDBM.dec_val);
                Print($"cnc_rdmacro success,Value:{value}");
            }
            else
            {
                Print($"cnc_rdmacro fail,ret:{_ret}");
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            short s_number = (short)this.num_MarcoNum.Value;
            short macroLength = (short)this.num_Length.Value;
            if (macroLength > 5)
            {
                MessageBox.Show("cnc_rdmacror length is greater than 5");
                return;
            }
            short e_number = (short)(s_number + macroLength-1);
            short length = (short)(8 + 8 * macroLength);//length不能超过5
            IODBMR iODBMR = new IODBMR();
            //对于系统宏变量来说可能每次只能读取一个宏变量的值
            _ret=Focas1.cnc_rdmacror(_flibhndl, s_number, e_number, length,  iODBMR);
            if (_ret == 0)
            {
                Print($"cnc_rdmacro success,datano_s:{iODBMR.datano_s},datano_e:{iODBMR.datano_e}");
                //用反射
                for (int i = 1;i<= macroLength; i++)
                {
                    var t = iODBMR.data.GetType();
                    var f= t.GetField($"data{i}");
                    IODBMR_data value =(IODBMR_data) f.GetValue(iODBMR.data);
                    if (value.dec_val == -1)
                    {
                        continue;
                    }
                    Print($"value{i}:{Math.Round(value.mcr_val * Math.Pow(10, -value.dec_val), value.dec_val)}");
                }

                //Print($"value1:{Math.Round(iODBMR.data.data1.mcr_val * Math.Pow(10, -iODBMR.data.data1.dec_val), iODBMR.data.data1.dec_val)}");
                //Print($"value2:{Math.Round(iODBMR.data.data2.mcr_val * Math.Pow(10, -iODBMR.data.data2.dec_val), iODBMR.data.data2.dec_val)}");
                //Print($"value3:{Math.Round(iODBMR.data.data3.mcr_val * Math.Pow(10, -iODBMR.data.data3.dec_val), iODBMR.data.data3.dec_val)}");
                //Print($"value4:{Math.Round(iODBMR.data.data4.mcr_val * Math.Pow(10, -iODBMR.data.data4.dec_val), iODBMR.data.data4.dec_val)}");
                //Print($"value5:{Math.Round(iODBMR.data.data5.mcr_val * Math.Pow(10, -iODBMR.data.data5.dec_val), iODBMR.data.data5.dec_val)}");
            }
            else
            {
                Print($"cnc_rdmacro fail,ret:{_ret}");
            }

        }

        private void button3_Click(object sender, EventArgs e)
        {
            int s_number = (short)this.num_MarcoNum.Value;
            int macroLength = (short)this.num_Length.Value;
            if (macroLength > 5)
            {
                MessageBox.Show("cnc_rdmacror length is greater than 5");
                return;
            }
            IODBMR iODBMR = new IODBMR();//有错误
            _ret = Focas1.cnc_rdmacror2(_flibhndl, s_number, ref macroLength, iODBMR);
            if( _ret == 0)
            {
                Print($"success");
            }
            else
            {
                Print($"cnc_rdmacror2 fail,ret:{_ret}");
            }
        }

        private void button4_Click(object sender, EventArgs e)
        {
            decimal writeValue = (decimal)this.num_marcoValue.Value;
            //如果值是1.2346789
            //writeValue = 1.23456m;
            short number = (short)this.num_MarcoNum.Value;
            short dec_val = GetDecimanlPlaces(writeValue);
            if(dec_val> 4) 
            { 
               MessageBox.Show("cnc_wrmacro value is greater than 65536");
                return;
            }
            short mcr_val = (short)(writeValue*(decimal)Math.Pow(10, dec_val));//不能大于65536

            _ret=Focas1.cnc_wrmacro(_flibhndl, number, 10, mcr_val, dec_val);
            if (_ret == 0)
            {
                Print($"cnc_wrmacro success");
            }
            else
            {
                Print($"cnc_wrmacro fail,ret:{_ret}");
            }
        }

     

        private void button5_Click(object sender, EventArgs e)
        {
            decimal writeValue = (decimal)this.num_marcoValue.Value;
            short s_number = (short)this.num_MarcoNum.Value;
            short macroLength = (short)this.num_Length.Value;
            if (macroLength > 5)
            {
                MessageBox.Show("cnc_rdmacror length is greater than 5");
                return;
            }
            short e_number = (short)(s_number + macroLength - 1);
            short length = (short)(8 + 8 * macroLength);//length不能超过5
            Focas1.IODBMR iODBMR = new Focas1.IODBMR();
            iODBMR.datano_s = s_number;
            iODBMR.datano_e = e_number;
            for (int i = 1; i <= macroLength; i++)
            {
                writeValue += 1;
                iODBMR.data.GetType().GetField($"data{i}")
                    .SetValue(iODBMR.data, new IODBMR_data() { 
                        dec_val = GetDecimanlPlaces(writeValue), 
                        mcr_val = (short)(writeValue * (decimal)Math.Pow(10, GetDecimanlPlaces(writeValue))) });
            }
            _ret= Focas1.cnc_wrmacror(_flibhndl, length, iODBMR);
            if (_ret == 0)
            {
                Print($"cnc_wrmacror success");
            }
            else
            {
                Print($"cnc_wrmacror fail,ret:{_ret}");
            }
        }

        /// <summary>
        /// 获取小数点位数
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        private short GetDecimanlPlaces(decimal value)
        {
            string valStr = value.ToString();
            var index = valStr.IndexOf(".");
            if (index == -1) return 0;
            return (short)(valStr.Length - index - 1);
        }

        private void btn_WriteToolGroup_Click(object sender, EventArgs e)
        {
            short length = 8 + 16 * 2;
            Focas1.IODBTGI iODBTGI = new Focas1.IODBTGI();
            iODBTGI.s_grp = (short)this.num_ToolGrpNo.Value;
            iODBTGI.e_grp = (short)(iODBTGI.s_grp+1);
            iODBTGI.data.data1.count_value = 100;
            iODBTGI.data.data1.counter = 10;
            iODBTGI.data.data1.count_type = 0;//0:count 1:minute

            iODBTGI.data.data2.count_value = 500;
            iODBTGI.data.data2.counter = 50;
            iODBTGI.data.data2.count_type = 1;//0:count 1:minute
            _ret = Focas1.cnc_wrgrpinfo(_flibhndl, length, iODBTGI);//无法新增刀具组 只能修改刀具组信息   
            if (_ret == 0)
            {
                Print($"cnc_wrgrpinfo success");
            }
            else
            {
                Print($"cnc_wrgrpinfo fail,ret:{_ret}");
            }
        }

        private void btn_InsertTool_Click(object sender, EventArgs e)
        {
            Focas1.IDBITD iDBITD = new Focas1.IDBITD();
            iDBITD.datano = (short)this.num_ToolGrpNo.Value; 
            iDBITD.type = 3;//在此刀具序号后面插入刀具
            iDBITD.data = (short)this.num_ToolNo.Value;
            //如何刀具组未注册则会自动注册
            _ret = Focas1.cnc_instlifedt(_flibhndl, iDBITD);
            if (_ret == 0)
            {
                Print($"cnc_instlifedt success");
            }
            else
            {
                Print($"cnc_instlifedt fail,ret:{_ret}");
            }
        }

        private void btn_WriteToolData_Click(object sender, EventArgs e)
        {
            Focas1.IODBTD iODBTD=new Focas1.IODBTD();
            iODBTD.datano= (short)this.num_ToolGrpNo.Value; ;
            iODBTD.type = (short)this.num_ToolNo.Value;
            iODBTD.tool_num = 50;
            iODBTD.h_code = 50;
            iODBTD.d_code = 50;
            iODBTD.tool_inf=this.cmb_ToolInfo.SelectedIndex+1;

            _ret = Focas1.cnc_wr1tlifedata(_flibhndl, iODBTD);
            if (_ret == 0)
            {
                Print($"cnc_wr1tlifedata success");
            }
            else
            {
                Print($"cnc_wr1tlifedata fail,ret:{_ret}");
            }
        }

        private void btn_ToolgroupInfo_Click(object sender, EventArgs e)
        {
            short s_grp=(short)this.num_ToolGrpNo.Value;
            short e_grp = (short)(s_grp+3);
            short length= (short)(8 + 16 * (e_grp - s_grp+1));
            Focas1.IODBTGI iODBTGI =new IODBTGI ();
            _ret= Focas1.cnc_rdgrpinfo(_flibhndl, s_grp, e_grp, length, iODBTGI);
            if (_ret == 0)
            {
                Print($"s_grp:{iODBTGI.s_grp},e_grp:{iODBTGI.e_grp}");
                for (int i = 1; i <= e_grp - s_grp + 1; i++)
                {
                    var data= (IODBTGI_data)iODBTGI.data.GetType().GetField($"data{i}").GetValue(iODBTGI.data);
                    Print($"group:{i},n_tool:{data.n_tool},count_type:{data.count_type},count_value:{data.count_value},counter:{data.counter}");
                }
            }
            else
            {
                Print($"cnc_rdgrpinfo fail,ret:{_ret}");
            }
        }

        private void btn_rdtool_Click(object sender, EventArgs e)
        {
            Focas1.IODBTD iODBTD = new Focas1.IODBTD();
            _ret=Focas1.cnc_rd1tlifedata(_flibhndl, (short)this.num_ToolGrpNo.Value, (short)this.num_ToolNo.Value, iODBTD);
            if (_ret == 0)
            {
                Print($"groupno:{iODBTD.datano},tool_num:{iODBTD.tool_num},h_code:{iODBTD.h_code},d_code:{iODBTD.d_code},tool_inf:{iODBTD.tool_inf}");
            }
            else
            {
                Print($"cnc_rd1tlifedata fail,ret:{_ret}");
            }
        }

        private void btn_rdtoolandtoolgrp_Click(object sender, EventArgs e)
        {
            short length = (short)(20 + 20 * 4);
            Focas1.ODBTG oDBTG = new Focas1.ODBTG();

            _ret=Focas1.cnc_rdtoolgrp(_flibhndl, (short)this.num_ToolGrpNo.Value, length, oDBTG);
            if (_ret == 0)
            {
                Print($"groupno:{oDBTG.grp_num},ntool:{oDBTG.ntool},life:{oDBTG.life},count:{oDBTG.count}");//少了个计数类型
                for (int i = 1; i <= oDBTG.ntool; i++)
                {
                    var data = (ODBTG_data)oDBTG.data.GetType().GetField($"data{i}").GetValue(oDBTG.data);
                    Print($"tuse_num:{data.tuse_num},toolno:{data.tool_num},h_code:{data.length_num},d_code:{data.radius_num},tool_inf:{data.tinfo}");
                }
            }
            else
            {
                Print($"cnc_rdtoolgrp fail,ret:{_ret}");
            }
        }

        private void btn_DelTool_Click(object sender, EventArgs e)
        {
           _ret= Focas1.cnc_deltlifedt(_flibhndl, (short)this.num_ToolGrpNo.Value, (short)this.num_ToolOrderNo.Value);
            if (_ret == 0)
            {
                Print($"cnc_deltlifedt success");
            }
            else
            {
                Print($"cnc_deltlifedt fail,ret:{_ret}");
            }
        }

        private void btn_DelToolGrp_Click(object sender, EventArgs e)
        {
            _ret = Focas1.cnc_deltlifegrp(_flibhndl, (short)this.num_ToolGrpNo.Value);
            if (_ret == 0)
            {
                Print($"cnc_deltlifegrp success");
            }
            else
            {
                Print($"cnc_deltlifegrp fail,ret:{_ret}");
            }
        }

        private void btn_rdmodaln4_Click(object sender, EventArgs e)
        {
            short type = -4;//Read all data of 1 shot G code at a time.
            short block = 0;//	active block
            ODBMDL_2 oDBMDL = new ODBMDL_2();
            _ret =Focas1.cnc_modal(_flibhndl, type, block, oDBMDL);
            if (_ret==0)
            {
                Print($"datano:{oDBMDL.datano},type:{oDBMDL.type}");
                for (int i = 0; i < 4; i++)
                {
                    byte value =(byte) (oDBMDL.g_1shot[i] & 0b0111_1111);//去低6位的数据
                    Print($"{i}:{value}");
                }

            }
            else
            {
                Print($"cnc_modal fail,ret:{_ret}");
            }
        }

        private void button6_Click_1(object sender, EventArgs e)
        {
            short type = -3;//Read all data concerning axis other than G code at a time.
            short block = 0;//	active block
            ODBMDL_5 oDBMDL = new ODBMDL_5();
            _ret = Focas1.cnc_modal(_flibhndl, type, block, oDBMDL);
            if (_ret == 0)
            {
                Print($"datano:{oDBMDL.datano},type:{oDBMDL.type}");
                for (int i = 1; i <= 8; i++)
                {
                    MODAL_AUX_data data=(MODAL_AUX_data) oDBMDL.raux2.GetType().GetField($"data{i}").GetValue(oDBMDL.raux2);
                    Print($"{i}:aux_data:{data.aux_data}");
                }

            }
            else
            {
                Print($"cnc_modal fail,ret:{_ret}");
            }
        }

        private void button13_Click(object sender, EventArgs e)
        {
            short type =(short)this.num_RdModalType.Value;//Read all data concerning axis other than G code one by one.
            short block = 0;//	active block
            ODBMDL_3 oDBMDL = new ODBMDL_3();
            _ret = Focas1.cnc_modal(_flibhndl, type, block, oDBMDL);
            if (_ret == 0)
            {
                Print($"datano:{oDBMDL.datano},type:{oDBMDL.type}");
                //MODAL_AUX_data data = (MODAL_AUX_data)oDBMDL.raux2.GetType().GetField($"data{i}").GetValue(oDBMDL.raux2);
                Print($"aux_data:{oDBMDL.aux.aux_data}");

            }
            else
            {
                Print($"cnc_modal fail,ret:{_ret}");
            }
        }

        private void button7_Click(object sender, EventArgs e)
        {
            short type = -2;//Read all data other than G code at a time
            short block = 0;//	active block
            ODBMDL_4 oDBMDL = new ODBMDL_4();
            _ret = Focas1.cnc_modal(_flibhndl, type, block, oDBMDL);
            if (_ret == 0)
            {
                Print($"datano:{oDBMDL.datano},type:{oDBMDL.type}");
                for (int i = 1; i <= 27; i++)
                {
                    MODAL_AUX_data data = (MODAL_AUX_data)oDBMDL.raux1.GetType().GetField($"data{i}").GetValue(oDBMDL.raux1);
                    Print($"{i}:aux_data:{data.aux_data}");
                }

            }
            else
            {
                Print($"cnc_modal fail,ret:{_ret}");
            }
        }

        private void button10_Click(object sender, EventArgs e)
        {
            short type = (short)this.num_RdModalType.Value;//Read other than G code one by one.
            short block = 0;//	active block
            ODBMDL_3 oDBMDL = new ODBMDL_3();
            _ret = Focas1.cnc_modal(_flibhndl, type, block, oDBMDL);
            if (_ret == 0)
            {
                Print($"datano:{oDBMDL.datano},type:{oDBMDL.type}");
                Print($"aux_data:{oDBMDL.aux.aux_data}");

            }
            else
            {
                Print($"cnc_modal fail,ret:{_ret}");
            }
        }

        private void button8_Click(object sender, EventArgs e)
        {
            short type = -1;//Read all data of G code at a time.
            short block = 0;//	active block
            ODBMDL_2 oDBMDL = new ODBMDL_2();
            _ret = Focas1.cnc_modal(_flibhndl, type, block, oDBMDL);
            if (_ret == 0)
            {
                Print($"datano:{oDBMDL.datano},type:{oDBMDL.type}");
                for (global::System.Int32 i = 0; i < 35; i++)
                {
                    byte value = (byte)(oDBMDL.g_rdata[i] & 0b0111_1111);//去低6位的数据
                    Print($"type:{i}:{value}");
                }

            }
            else
            {
                Print($"cnc_modal fail,ret:{_ret}");
            }
        }

        private void button9_Click(object sender, EventArgs e)
        {
            short type = (short)this.num_RdModalType.Value;//Read  G code one by one.
            short block = 0;//	active block
            ODBMDL_1 oDBMDL = new ODBMDL_1();
            _ret = Focas1.cnc_modal(_flibhndl, type, block, oDBMDL);
            if (_ret == 0)
            {
                Print($"datano:{oDBMDL.datano},type:{oDBMDL.type}");
                byte value = (byte)(oDBMDL.g_data & 0b0111_1111);//去低6位的数据
                Print($"aux_data:{value}");

            }
            else
            {
                Print($"cnc_modal fail,ret:{_ret}");
            }
        }

        private void button12_Click(object sender, EventArgs e)
        {
            short type = (short)this.num_RdModalType.Value;//Read 1 shot G code one by one.
            short block = 0;//	active block
            ODBMDL_1 oDBMDL = new ODBMDL_1();
            _ret = Focas1.cnc_modal(_flibhndl, type, block, oDBMDL);
            if (_ret == 0)
            {
                Print($"datano:{oDBMDL.datano},type:{oDBMDL.type}");
                byte value = (byte)(oDBMDL.g_data & 0b0111_1111);//去低6位的数据
                Print($"aux_data:{value}");

            }
            else
            {
                Print($"cnc_modal fail,ret:{_ret}");
            }
            //优点就是全  缺点需要对照表
        }

        private void button11_Click(object sender, EventArgs e)
        {
            short type = -1;
            short block = 1;//1 active block    
            short num_gcode = 20;//the number of code
            ODBGCD oDBGCD = new ODBGCD();
            try
            {
                _ret = Focas1.cnc_rdgcode(_flibhndl, type, block, ref num_gcode, oDBGCD);
                if (_ret == 0)
                {
                    Print($"num_gcode:{num_gcode}");
                    for (int i = 0; i < num_gcode; i++)
                    {
                        var data = (ODBGCD_data)oDBGCD.GetType().GetField($"gcd{i}").GetValue(oDBGCD);
                        Print($"group:{data.group},flag:{data.flag},code:{data.code}");
                    }
                }
                else
                {
                    Print($"cnc_rdgcode fail,ret:{_ret}");
                }
            }
            catch (Exception ex)
            {
                //如果一次性读取的数据量大于28个，程序会直接奔溃而不会被捕获到异常，原因是此异常发生成C代码内部
                Print(ex.ToString());
            }
          
        }

        private void button15_Click(object sender, EventArgs e)
        {
            short type = (short)this.num_RdModalType.Value; ;//Read  G code one by one.
            short block = 2;
            short num_gcode = 1;//数量只能是1
            ODBGCD oDBGCD = new ODBGCD();
            //G17 G02 当前行
            //G01

            //#3 = 1	:	command differ from the previous block (only Series 30i, 0i-D/F, PMi-A)
            //#7 = 1	:	There is a command in the present block.   command:在此block被声明，此行有这个命令，而非通过模态继承的
            //type:0,block=1    flag #3:1    #7:1     136  G02
            //type:1,block=1    flag #3:0    #7:1     128  G17
            //type:0,block=2    flag #3:0    #7:1     128  G01
            //type:1,block=2    flag #3:0    #7:0     0  G01

            _ret = Focas1.cnc_rdgcode(_flibhndl, type, block, ref num_gcode, oDBGCD);
            if (_ret == 0)
            {
                Print($"num_gcode:{num_gcode}");
                for (int i = 0; i < num_gcode; i++)
                {
                    var data = (ODBGCD_data)oDBGCD.GetType().GetField($"gcd{i}").GetValue(oDBGCD);
                    Print($"group:{data.group},flag:{data.flag},code:{data.code}");
                }
            }
            else
            {
                Print($"cnc_rdgcode fail,ret:{_ret}");
            }
        }

        private void button14_Click(object sender, EventArgs e)
        {
            short type = -2;
            short block = 1;//1 active block    
            short num_gcode = 28;//the number of code
            ODBGCD oDBGCD = new ODBGCD();
            try
            {
                _ret = Focas1.cnc_rdgcode(_flibhndl, type, block, ref num_gcode, oDBGCD);
                if (_ret == 0)
                {
                    Print($"num_gcode:{num_gcode}");
                    for (int i = 0; i < num_gcode; i++)
                    {
                        var data = (ODBGCD_data)oDBGCD.GetType().GetField($"gcd{i}").GetValue(oDBGCD);
                        Print($"group:{data.group},flag:{data.flag},code:{data.code}");
                    }
                }
                else
                {
                    Print($"cnc_rdgcode fail,ret:{_ret}");
                }
            }
            catch (Exception ex)
            {
                //如果一次性读取的数据量大于28个，程序会直接奔溃而不会被捕获到异常，原因是此异常发生成C代码内部
                Print(ex.ToString());
            }
        }

        private void button16_Click(object sender, EventArgs e)
        {
            short type = (short)this.num_RdModalType.Value; ;//Read  G code one by one.
            short block = 1;
            short num_gcode = 1;//数量只能是1
            ODBGCD oDBGCD = new ODBGCD();

            _ret = Focas1.cnc_rdgcode(_flibhndl, type, block, ref num_gcode, oDBGCD);
            if (_ret == 0)
            {
                Print($"num_gcode:{num_gcode}");
                for (int i = 0; i < num_gcode; i++)
                {
                    var data = (ODBGCD_data)oDBGCD.GetType().GetField($"gcd{i}").GetValue(oDBGCD);
                    Print($"group:{data.group},flag:{data.flag},code:{data.code}");
                }
            }
            else
            {
                Print($"cnc_rdgcode fail,ret:{_ret}");
            }
        }

        private void button20_Click(object sender, EventArgs e)
        {
            short type = -1;//Reading the modal data except G code at a time
            short block = 1;//1 active block    
            short num_gcode = 30;//the number of code
            ODBCMD oDBGCD = new ODBCMD();
            _ret = Focas1.cnc_rdcommand(_flibhndl, type, block, ref num_gcode, oDBGCD);
            if (_ret == 0)
            {
                Print($"num_gcode:{num_gcode}");
                for (int i = 0; i < num_gcode; i++)
                {
                    var data = (ODBCMD_data)oDBGCD.GetType().GetField($"cmd{i}").GetValue(oDBGCD);
                    Print($"adrs:{(char)data.adrs},num:{data.num},flag:{data.flag},cmd_val:{data.cmd_val},dec_val:{data.dec_val}");
                }
            }
            else
            {
                Print($"cnc_rdcommand fail,ret:{_ret}");
            }
        }

        private void button18_Click(object sender, EventArgs e)
        {
            short type = (short)this.num_RdModalType.Value; //Reading the modal data except G code one by one
            short block = 1;//1 active block    
            short num_gcode = 1;//the number of code
            ODBCMD oDBGCD = new ODBCMD();
            _ret = Focas1.cnc_rdcommand(_flibhndl, type, block, ref num_gcode, oDBGCD);
            if (_ret == 0)
            {
                Print($"num_gcode:{num_gcode}");
                for (int i = 0; i < num_gcode; i++)
                {
                    var data = (ODBCMD_data)oDBGCD.GetType().GetField($"cmd{i}").GetValue(oDBGCD);
                    Print($"adrs:{(char)data.adrs},num:{data.num},flag:{data.flag},cmd_val:{data.cmd_val},dec_val:{data.dec_val}");
                }
            }
            else
            {
                Print($"cnc_rdcommand fail,ret:{_ret}");
            }
        }

        private void button19_Click(object sender, EventArgs e)
        {
            short type = -2;//Reading commanded data at a time
            short block = 1;//1 active block    
            short num_gcode = 30;//the number of code
            ODBCMD oDBGCD = new ODBCMD();
            _ret = Focas1.cnc_rdcommand(_flibhndl, type, block, ref num_gcode, oDBGCD);
            if (_ret == 0)
            {
                Print($"num_gcode:{num_gcode}");
                for (int i = 0; i < num_gcode; i++)
                {
                    var data = (ODBCMD_data)oDBGCD.GetType().GetField($"cmd{i}").GetValue(oDBGCD);
                    double value = data.cmd_val * Math.Pow(10, -data.dec_val);
                    Print($"adrs:{(char)data.adrs},num:{data.num},flag:{data.flag},value:{value}");
                }
            }
            else
            {
                Print($"cnc_rdcommand fail,ret:{_ret}");
            }
        }

        private void button17_Click(object sender, EventArgs e)
        {
            short type = (short)this.num_RdModalType.Value; //Reading commanded data one by one
            short block = 1;//1 active block    
            short num_gcode = 1;//the number of code
            ODBCMD oDBGCD = new ODBCMD();
            _ret = Focas1.cnc_rdcommand(_flibhndl, type, block, ref num_gcode, oDBGCD);
            if (_ret == 0)
            {
                Print($"num_gcode:{num_gcode}");
                for (int i = 0; i < num_gcode; i++)
                {
                    var data = (ODBCMD_data)oDBGCD.GetType().GetField($"cmd{i}").GetValue(oDBGCD);
                    double value = data.cmd_val * Math.Pow(10, -data.dec_val);
                    Print($"adrs:{(char)data.adrs},num:{data.num},flag:{data.flag},value:{value}");
                }
            }
            else
            {
                Print($"cnc_rdcommand fail,ret:{_ret}");
            }
        }

        private void button22_Click(object sender, EventArgs e)
        {
            short type = -3;//Reading commanded data concerning axis at a time
            short block = 1;//1 active block    
            short num_gcode = 30;//the number of code
            ODBCMD oDBGCD = new ODBCMD();
            _ret = Focas1.cnc_rdcommand(_flibhndl, type, block, ref num_gcode, oDBGCD);
            if (_ret == 0)
            {
                Print($"num_gcode:{num_gcode}");
                for (int i = 0; i < num_gcode; i++)
                {
                    var data = (ODBCMD_data)oDBGCD.GetType().GetField($"cmd{i}").GetValue(oDBGCD);
                    double value = data.cmd_val * Math.Pow(10, -data.dec_val);
                    Print($"adrs:{(char)data.adrs},num:{data.num},flag:{data.flag},value:{value}");
                }
            }
            else
            {
                Print($"cnc_rdcommand fail,ret:{_ret}");
            }
        }

        private void button21_Click(object sender, EventArgs e)
        {
            short type = (short)this.num_RdModalType.Value; //Reading commanded data concerning axis one by one
            short block = 1;//1 active block    
            short num_gcode = 1;//the number of code
            ODBCMD oDBGCD = new ODBCMD();
            _ret = Focas1.cnc_rdcommand(_flibhndl, type, block, ref num_gcode, oDBGCD);
            if (_ret == 0)
            {
                Print($"num_gcode:{num_gcode}");
                for (int i = 0; i < num_gcode; i++)
                {
                    var data = (ODBCMD_data)oDBGCD.GetType().GetField($"cmd{i}").GetValue(oDBGCD);
                    double value = data.cmd_val * Math.Pow(10, -data.dec_val);
                    Print($"adrs:{(char)data.adrs},num:{data.num},flag:{data.flag},value:{value}");
                }
            }
            else
            {
                Print($"cnc_rdcommand fail,ret:{_ret}");
            }
        }

        private void btn_SetPath_Click(object sender, EventArgs e)
        {
            short path = (short)this.num_Path.Value;
            _ret = Focas1.cnc_setpath(_flibhndl, path);
            if (_ret == 0)
            {
                Print($"cnc_setpath success");
            }
            else
            {
                Print($"cnc_setpath fail,ret:{_ret}");
            }
        }

        private void btn_rdtimer_Click(object sender, EventArgs e)
        {
            short type=(short)this.cmb_TimerType.SelectedIndex;
            IODBTIME iODBTIME = new IODBTIME();
            _ret= Focas1.cnc_rdtimer(_flibhndl, type, iODBTIME);
            if (_ret == 0)
            {
                Print($"iODBTIME.minute:{iODBTIME.minute},iODBTIME.msec:{iODBTIME.msec}");
                int minute =BitConverter.ToInt32( BitConverter.GetBytes(iODBTIME.minute).Reverse().ToArray(),0);
                int msec= BitConverter.ToInt32(BitConverter.GetBytes(iODBTIME.msec).Reverse().ToArray(), 0);
                var hour = minute / 60;
                minute = minute % 60;
                var second = msec / 1000;
        
                Print($"hour:{hour},minute:{minute},second:{second}");
            }
            else
            {
                Print($"cnc_rdtimer fail,ret:{_ret}");
            }
        }

        private void btn_writeTimer_Click(object sender, EventArgs e)
        {
            short type = (short)this.cmb_TimerType.SelectedIndex;
            IODBTIME iODBTIME = new IODBTIME();
            int minute = (int)this.num_Minute.Value;
            int msec = (int)this.num_MSec.Value;
            iODBTIME.minute = BitConverter.ToInt32(System.BitConverter.GetBytes(minute).Reverse().ToArray(), 0);     //接受的是大端   原因是机器传过来是小端字节序  但focas解析minute和msec的时候还是以大端去解析的 导致的错误
            iODBTIME.msec = BitConverter.ToInt32(System.BitConverter.GetBytes(msec).Reverse().ToArray(), 0);
            _ret = Focas1.cnc_wrtimer(_flibhndl, type, iODBTIME);
            if (_ret == 0)
            {
                Print($"cnc_wrtimer success");


            }
            else
            {
                Print($"cnc_wrtimer fail,ret:{_ret}");
            }
        }

        private void btn_getsystime_Click(object sender, EventArgs e)
        {
            IODBTIMER iODBTIMER = new IODBTIMER();
            iODBTIMER.type=0;
            _ret = Focas1.cnc_gettimer(_flibhndl, iODBTIMER);
            if (_ret == 0)
            {
                Print($"{iODBTIMER.date.year}-{iODBTIMER.date.month}-{iODBTIMER.date.date}");
                iODBTIMER.type = 1;
                _ret = Focas1.cnc_gettimer(_flibhndl, iODBTIMER);
                if( _ret == 0)
                {
                    Print($"{iODBTIMER.time.hour}-{iODBTIMER.time.minute}-{iODBTIMER.time.second}");
                }
                else
                {
                    Print($"cnc_wrtimer fail,ret:{_ret}");
                }

            }
            else
            {
                Print($"cnc_wrtimer fail,ret:{_ret}");
            }
        }

        private void btn_setsystemTimer_Click(object sender, EventArgs e)
        {
            IODBTIMER iODBTIMER = new IODBTIMER();
            iODBTIMER.type=0;

            iODBTIMER.date.year = 2025;
            iODBTIMER.date.month = 5;
            iODBTIMER.date.date = 21;
            _ret=Focas1.cnc_settimer(_flibhndl, iODBTIMER);
            if (_ret == 0)
            {
                Print($"cnc_settimer success");


            }
            else
            {
                Print($"cnc_settimer fail,ret:{_ret}");
            }
            iODBTIMER.type = 1;

            iODBTIMER.time.hour = 11;
            iODBTIMER.time.minute = 5;
            iODBTIMER.time.second = 0;
            _ret = Focas1.cnc_settimer(_flibhndl, iODBTIMER);
            if (_ret == 0)
            {
                Print($"cnc_settimer success");
            }
            else
            {
                Print($"cnc_settimer fail,ret:{_ret}");
            }

            //虚拟机里面可能有时钟同步  自动通过NTP同步了
            //虚拟机就不让设置系统时间
        }

        private void btn_RdOpMsg_Click(object sender, EventArgs e)
        {
            ushort s_no = (ushort)this.num_RdOpHisSNo.Value;
            ushort e_no = (ushort)this.num_RdOpHisENo.Value;
            ushort length = (ushort)(e_no - s_no + 1);
            length = (ushort)(520 * length);

            _ret = Focas1.cnc_stopophis(_flibhndl);
            if (_ret != 0)
            {
                Print($"cnc_stopophis fail,ret:{_ret}");
                return;
            }

            _ret = Focas1.cnc_rdophisno(_flibhndl, out ushort hisno);
            if (_ret != 0)
            {
                Print($"cnc_rdophisno fail,ret:{_ret}");
                return;
            }

            Print($"cnc_rdophisno,success:{hisno}");

            //ODBHIS oDBHIS = new ODBHIS();
            //_ret = Focas1.cnc_rdophistry3(_flibhndl, s_no,ref e_no,ref length, oDBHIS);
            //if (_ret == 0)
            //{
            //    Print($"cnc_rdopmsg success");
            //    //Print($"oPMSG.datano:{oPMSG.msg1.datano},oPMSG.type:{oPMSG.msg1.type},oPMSG.char_num:{oPMSG.msg1.char_num},oPMSG.data:{oPMSG.msg1.data}");
            //    //Print($"oPMSG.datano:{oPMSG.msg2.datano},oPMSG.type:{oPMSG.msg2.type},oPMSG.char_num:{oPMSG.msg2.char_num},oPMSG.data:{oPMSG.msg2.data}");
            //    //Print($"oPMSG.datano:{oPMSG.msg3.datano},oPMSG.type:{oPMSG.msg3.type},oPMSG.char_num:{oPMSG.msg3.char_num},oPMSG.data:{oPMSG.msg3.data}");
            //    //Print($"oPMSG.datano:{oPMSG.msg4.datano},oPMSG.type:{oPMSG.msg4.type},oPMSG.char_num:{oPMSG.msg4.char_num},oPMSG.data:{oPMSG.msg4.data}");
            //    //Print($"oPMSG.datano:{oPMSG.msg5.datano},oPMSG.type:{oPMSG.msg5.type} ,oPMSG.char_num: {oPMSG.msg5.char_num} ,oPMSG.data: {oPMSG.msg5.data}");
            //}
            //else
            //{
            //    Print($"cnc_rdophistry fail,ret:{_ret}");
            //}
            //Thread.Sleep(10);


            var oDBHIS1 = new ODBOPHIS4_1();
            var oDBHIS2 = new ODBOPHIS4_2();
            var oDBHIS3 = new ODBOPHIS4_3();
            var oDBHIS4 = new ODBOPHIS4_4();
            var oDBHIS5 = new ODBOPHIS4_5();
            var oDBHIS6 = new ODBOPHIS4_6();
            var oDBHIS7 = new ODBOPHIS4_7();
            var oDBHIS8 = new ODBOPHIS4_8();
            var oDBHIS9 = new ODBOPHIS4_9();
            var oDBHIS10 = new ODBOPHIS4_10();
            var oDBHIS11 = new ODBOPHIS4_11();

            try
            {
                _ret = Focas1.cnc_rdophistry4(_flibhndl, s_no, ref e_no, ref length, oDBHIS1);//报错
            }
            catch(Exception ex)
            {
                Print($"cnc_rdophistry4 fail,ret:{_ret}");
            }
            if (_ret == 0)
            {//有可能很多种类型一起返回回来导致没法解析
                Print($"e_no:{e_no},length:{length}");
                for (int i = 1; i <= 10; i++)
                {
                    var data = (REC_MDI4_data)oDBHIS1.GetType().GetField($"rec_mdi{i}").GetValue(oDBHIS1);
                    if (data.rec_type != 0)
                    {
                        continue;
                    }
                    Print($"{i}:length:{data.rec_len},type:{data.rec_type},key_code:{GetKeyByKeyCode(data.data.key_code)},time:{data.data.hour}:{data.data.minute}:{data.data.second}");
                }
            }
            else
            {
                Print($"cnc_rdophistry fail,ret:{_ret}");
            }
            _ret = Focas1.cnc_rdophistry4(_flibhndl, s_no, ref e_no, ref length, oDBHIS2);
            if (_ret == 0)
            {
                if (oDBHIS2.rec_sgn1.rec_type == 1)
                {
                    REC_SGN4 data = oDBHIS2.rec_sgn1.data;//0 32就是第5位
                    Print($"{s_no}:length:{oDBHIS2.rec_sgn1.rec_len},type:{oDBHIS2.rec_sgn1.rec_type},sig_name:{data.sig_name},sig_no:{data.sig_no},sig_old:{data.sig_old},sig_new:{data.sig_new},pmc_no:{data.pmc_no},time:{data.hour}:{data.minute}:{data.second}");
                }
            }
            else
            {
                Print($"cnc_rdophistry4 oDBHIS2 error:{_ret}");
            }

            _ret = Focas1.cnc_rdophistry4(_flibhndl, s_no, ref e_no, ref length, oDBHIS3);
            if (_ret == 0)
            {
                if (oDBHIS3.rec_alm1.rec_type == 2)
                {
                    REC_ALM4 data = oDBHIS3.rec_alm1.data;
                    Print($"rec_len:{oDBHIS3.rec_alm1.rec_len}.rec_type:{oDBHIS3.rec_alm1.rec_type}, " +
                        $"alm_grp:{data.alm_grp},alm_no:{data.alm_no},axis_no:{data.axis_no},Time:{data.year}-{data.month}-{data.day}{data.hour}:{data.minute}:{data.second},pth_no:{data.pth_no}");
                }
            }
            else
            {
                Print($"cnc_rdophistry4 oDBHIS3 error:{_ret}");
            }

            _ret = Focas1.cnc_rdophistry4(_flibhndl, s_no, ref e_no, ref length, oDBHIS4);
            if (_ret == 0)
            {
                if(oDBHIS4.rec_date1.rec_type==3)
                {
                    REC_DATE4 data = oDBHIS4.rec_date1.data;
                    Print($"evnt_type:{data.evnt_type},Time:{data.year}-{data.month}-{data.day} {data.hour}:{data.minute}:{data.second}");
                }
            }
            else
            {
                Print($"cnc_rdophistry4 oDBHIS4 error:{_ret}");
            }

            _ret = Focas1.cnc_rdophistry4(_flibhndl, s_no, ref e_no, ref length, oDBHIS5);
            if (_ret == 0)
            {
                for (int i = 1; i <= 10; i++)
                {
         
                    var data = (REC_IAL4_data)oDBHIS5.GetType().GetField($"rec_ial{i}").GetValue(oDBHIS5);
                    if (data.rec_type == 9)
                    {
                        Print($"{i}:length:{data.rec_len},type:{data.rec_type},alm_no:{data.data.alm_no},alm_grp:{data.data.alm_grp}");
                    }
                }
            }
            else
            {
                Print($"cnc_rdophistry4 oDBHIS5 error:{_ret}");
            }

            _ret = Focas1.cnc_rdophistry4(_flibhndl, s_no, ref e_no, ref length, oDBHIS6);
            if (_ret == 0)
            {
                if (oDBHIS6.rec_mal1.rec_type == 10)
                {
                    REC_MAL4 data = oDBHIS6.rec_mal1.data;
                    Print($"{s_no}:length:{oDBHIS6.rec_mal1.rec_len},type:{oDBHIS6.rec_mal1.rec_type},alm_grp:{data.alm_grp},alm_no:{data.alm_no},time:{data.year}-{data.month}-{data.day} {data.hour}:{data.minute}:{data.second},message:{data.alm_msg}");
                }
            }
            else
            {
                Print($"cnc_rdophistry4 oDBHIS6 error:{_ret}");
            }

            _ret = Focas1.cnc_rdophistry4(_flibhndl, s_no, ref e_no, ref length, oDBHIS7);
            if (_ret == 0)
            {
                if(oDBHIS7.rec_opm1.rec_type==6)
                {
                    REC_OPM4 data = oDBHIS7.rec_opm1.data;
                    Print($"{s_no}:length:{oDBHIS7.rec_opm1.rec_len},type:{oDBHIS7.rec_opm1.rec_type},dsp_flg:{data.dsp_flg},om_no:{data.om_no},time:{data.year}-{data.month}-{data.day} {data.hour}:{data.minute}:{data.second},message:{data.ope_msg}");
                }
            }
            else
            {
                Print($"cnc_rdophistry4 oDBHIS7 error:{_ret}");
            }

            _ret = Focas1.cnc_rdophistry4(_flibhndl, s_no, ref e_no, ref length, oDBHIS8);
            if (_ret == 0)
            {
                if (oDBHIS8.rec_ofs1.rec_type == 7)
                {
                    REC_OFS4 data = oDBHIS8.rec_ofs1.data;
                    Print($"rec_len:{oDBHIS8.rec_ofs1.rec_len}.rec_type:{oDBHIS8.rec_ofs1.rec_type}, ofs_grp:{data.ofs_grp},ofs_no:{data.ofs_no},Time:{data.hour}:{data.minute}:{data.second},pth_no:{data.pth_no},ofs_old:{data.ofs_old},ofs_new:{data.ofs_new},old_dp:{data.old_dp},new_dp:{data.new_dp}");
                }
             
            }
            else
            {
                Print($"cnc_rdophistry4 oDBHIS8 error:{_ret}");
            }

            _ret = Focas1.cnc_rdophistry4(_flibhndl, s_no, ref e_no, ref length, oDBHIS9);
            if (_ret == 0)
            {
                if (oDBHIS9.rec_prm1.rec_type == 5)
                {
                    REC_PRM4 data = oDBHIS9.rec_prm1.data;
                    Print($"rec_len:{oDBHIS9.rec_prm1.rec_len}.rec_type:{oDBHIS9.rec_prm1.rec_type}, prm_grp:{data.prm_grp},prm_num:{data.prm_num},Time:{data.hour}:{data.minute}:{data.second},prm_len:{data.prm_len},prm_num:{data.prm_num},prm_old:{data.prm_old},prm_new:{data.prm_new},old_dp:{data.old_dp},new_dp:{data.new_dp}");
                }
            }
            else
            {
                Print($"cnc_rdophistry4 oDBHIS9 error:{_ret}");
            }

            _ret = Focas1.cnc_rdophistry4(_flibhndl, s_no, ref e_no, ref length, oDBHIS10);
            if (_ret == 0)
            {
                if (oDBHIS10.rec_wof1.rec_type==8)
                {
                    REC_WOF4 data = oDBHIS10.rec_wof1.data;
                    Print($"rec_len:{oDBHIS10.rec_wof1.rec_len}.rec_type:{oDBHIS10.rec_wof1.rec_type}, " +
                        $"ofs_grp:{data.ofs_grp},ofs_no:{data.ofs_no},Time:{data.hour}:{data.minute}:{data.second},pth_no:{data.pth_no},axis_no:{data.axis_no},ofs_old:{data.ofs_old},ofs_new:{data.ofs_new},old_dp:{data.old_dp},new_dp:{data.new_dp}");
                }
            }
            else
            {
                Print($"cnc_rdophistry4 oDBHIS10 error:{_ret}");
            }

            _ret = Focas1.cnc_rdophistry4(_flibhndl, s_no, ref e_no, ref length, oDBHIS11);
            if (_ret == 0)
            {
                if (oDBHIS11.rec_mac1.rec_type == 4)
                {
                    REC_MAC4 data = oDBHIS11.rec_mac1.data;
                    Print($"rec_len:{oDBHIS11.rec_mac1.rec_len}.rec_type:{oDBHIS11.rec_mac1.rec_type}, " +
                        $"mac_no:{data.mac_no},Time:{data.hour}:{data.minute}:{data.second},pth_no:{data.pth_no},mac_old:{data.mac_old},mac_new:{data.mac_new},old_dp:{data.old_dp},new_dp:{data.new_dp}");
                }
            }
            else
            {
                Print($"cnc_rdophistry4 oDBHIS11 error:{_ret}");
            }

         

            _ret = Focas1.cnc_startophis(_flibhndl);//在stop期间还记录吗？
            if (_ret != 0)
            {
                Print($"cnc_startophis fail,ret:{_ret}");
                return;
            }
            else
            {
                Print($"cnc_startophis success");

            }

        }

        private void button23_Click(object sender, EventArgs e)
        {
         
            Print($"cnc_stopophis success");
        }
        private void btn_StartOpHis_Click(object sender, EventArgs e)
        {
            _ret = Focas1.cnc_startophis(_flibhndl);//在stop期间还记录吗？
            if (_ret != 0)
            {
                Print($"cnc_startophis fail,ret:{_ret}");
                return;
            }
            else
            {
                Print($"cnc_startophis success");

            }
        }
        /// <summary>
        /// 根据按键码获取按键名称
        /// </summary>
        /// <param name="keyCode"></param>
        /// <returns></returns>
        private string GetKeyByKeyCode(byte keyCode)
        {
            // 高位 = keyCode >> 4, 低位 = keyCode & 0x0F
            switch (keyCode)
            {
                // ---- 第一张表格区域 (高四位0x0-0x7) ----
                // 高四位 0x0
                case 0x09: return "TAB";   // 列0 + 行9
                case 0x0A: return "EOB";   // 列0 + 行A

                // 高四位 0x2
                case 0x20: return "SP";    // 空格
                case 0x21: return "!";
                case 0x22: return "\"";    // 双引号
                case 0x23: return "#";
                case 0x24: return "$";
                case 0x25: return "%";
                case 0x26: return "&";
                case 0x27: return "'";
                case 0x28: return "(";
                case 0x29: return ")";
                case 0x2A: return "*";
                case 0x2B: return "+";
                case 0x2C: return ",";
                case 0x2D: return "-";
                case 0x2E: return ".";
                case 0x2F: return "/";

                // 高四位 0x3
                case 0x30: return "0";
                case 0x31: return "1";
                case 0x32: return "2";
                case 0x33: return "3";
                case 0x34: return "4";
                case 0x35: return "5";
                case 0x36: return "6";
                case 0x37: return "7";
                case 0x38: return "8";
                case 0x39: return "9";
                case 0x3A: return ":";
                case 0x3B: return ";";
                case 0x3C: return "<";
                case 0x3D: return "=";
                case 0x3E: return ">";
                case 0x3F: return "?";

                // 高四位 0x4
                case 0x40: return "@";
                case 0x41: return "A";
                case 0x42: return "B";
                case 0x43: return "C";
                case 0x44: return "D";
                case 0x45: return "E";
                case 0x46: return "F";
                case 0x47: return "G";
                case 0x48: return "H";
                case 0x49: return "I";
                case 0x4A: return "J";
                case 0x4B: return "K";
                case 0x4C: return "L";
                case 0x4D: return "M";
                case 0x4E: return "N";
                case 0x4F: return "O";

                // 高四位 0x5
                case 0x50: return "P";
                case 0x51: return "Q";
                case 0x52: return "R";
                case 0x53: return "S";
                case 0x54: return "T";
                case 0x55: return "U";
                case 0x56: return "V";
                case 0x57: return "W";
                case 0x58: return "X";
                case 0x59: return "Y";
                case 0x5A: return "Z";
                case 0x5B: return "[";
                case 0x5C: return "\\";   // 反斜杠
                case 0x5D: return "]";
                case 0x5F: return "_";

                // 高四位 0x6
                case 0x60: return "`";

                // 高四位 0x7
                case 0x7B: return "{";
                case 0x7C: return "|";
                case 0x7D: return "}";
                case 0x7E: return "~";

                // ---- 第二张表格区域 (高四位0x8-0xF) ----
                // 高四位 0x8
                case 0x84: return "SHIFT";
                case 0x86: return "CAN";
                case 0x88: return "<Cursor Right>";
                case 0x89: return "<Cursor Left>";
                case 0x8A: return "<Cursor Down>";
                case 0x8B: return "<Cursor Up>";
                case 0x8E: return "Page?";  // 特殊分页键
                case 0x8F: return "Page?";  // 特殊分页键

                // 高四位 0x9
                case 0x90: return "RESET";
                case 0x94: return "INSERT";
                case 0x95: return "DELETE";
                case 0x96: return "ALTER";
                case 0x97: return "ALT";
                case 0x98: return "INPUT";
                case 0x99: return "CALC";
                case 0x9A: return "HELP";
                case 0x9B: return "CTRL";
                case 0x9C: return "ABC/abc";  // 字母大小写切换

                // 高四位 0xA
                case 0xA0: return "[SKV9]";
                case 0xA1: return "[SKV8]";
                case 0xA2: return "[SKV7]";
                case 0xA3: return "[SKV6]";
                case 0xA4: return "[SKV5]";
                case 0xA5: return "[SKV4]";
                case 0xA6: return "[SKV3]";
                case 0xA7: return "[SKV2]";
                case 0xA8: return "[SKV1]";

                // 高四位 0xE
                case 0xE4: return "AUX";
                case 0xE8: return "POS";
                case 0xE9: return "PROG";
                case 0xEA: return "OFFSET";
                case 0xEB: return "SYSTEM";
                case 0xEC: return "MESSAGE";
                case 0xED: return "GRAPH";
                case 0xEE: return "CUSTOM";
                case 0xEF: return "CUSTOM2";

                // 高四位 0xF
                case 0xF0: return "[F1]";
                case 0xF1: return "[F2]";
                case 0xF2: return "[F3]";
                case 0xF3: return "[F4]";
                case 0xF4: return "[F5]";
                case 0xF5: return "[F6]";
                case 0xF6: return "[F7]";
                case 0xF7: return "[F8]";
                case 0xF8: return "[F9]";
                case 0xF9: return "[F10]";
                case 0xFE: return "[FR]";  // 右侧功能键
                case 0xFF: return "[FL]";  // 左侧功能键

                // 未定义的键码返回空字符串
                default: return "";
            }
        }

    
    }
}
