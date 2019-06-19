using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ControlLibrary
{
    public struct DataForm
    {
        public bool bRS; 
        public string buffer;
        public int iLength; //字节长度
        public string ipPort;// 从哪里接收，或发送至何方
        public DateTime dt;

        public void SetValue(bool rs, string content,string ip,int length)
        {
            bRS = rs; //false 接收 true 发送
            ipPort = ip;
            buffer = content;
            iLength = length;
            dt = DateTime.Now;
        }
    }
}
