﻿using System;
using System.Collections.Generic;
using System.Net;
using System.Timers;

namespace DataService
{
    public class PLCGroup : IGroup
    {
        protected Timer _timer;

        protected bool _isActive;
        public virtual bool IsActive
        {
            get
            {
                return _isActive;
            }
            set
            {
                _isActive = value;
                if (value)
                {
                    //这里设置组的更新周期
                    if (_updateRate <= 0) _updateRate = 100;
                    _timer.Interval = _updateRate;
                    _timer.Elapsed += new ElapsedEventHandler(timer_Timer);
                    _timer.Start();
                }
                else
                {
                    _timer.Elapsed -= new ElapsedEventHandler(timer_Timer);
                    _timer.Stop();
                }
            }
        }

        /// <summary>
        /// 代表该组的Group
        /// </summary>
        protected short _id;
        public short ID
        {
            get
            {
                return _id;
            }
        }

        protected int _updateRate;
        public int UpdateRate
        {
            get
            {
                return _updateRate;
            }
            set
            {
                _updateRate = value;
            }
        }

        protected string _name;
        public string Name
        {
            get
            {
                return _name;
            }
            set
            {
                _name = value;
            }
        }

        protected float _deadband;
        public float DeadBand
        {
            get
            {
                return _deadband;
            }
            set
            {
                _deadband = value;
            }
        }

        /// <summary>
        /// 这是在内存缓存区中的数据，和下位机发过来的数据进行对比，判断是否有更新
        /// </summary>
        protected ICache _cacheReader;

        /// <summary>
        /// 这个是从外部赋值的接口，代表要实现的不同驱动
        /// </summary>
        protected IPLCDriver _plcReader;
        public IDriver Parent
        {
            get
            {
                return _plcReader;
            }
        }

        protected List<int> _changedList;
        public List<int> ChangedList
        {
            get
            {
                return _changedList;
            }
        }


        protected List<ITag> _items;
        public IEnumerable<ITag> Items
        {
            get { return _items; }
        }

        protected IDataServer _server;
        public IDataServer Server
        {
            get
            {
                return _server;
            }
        }

        /// <summary>
        /// 确保在一起的 Itag 列表块地址，最大不超过PDU的大小
        /// </summary>
        protected List<PDUArea> _rangeList = new List<PDUArea>();

        protected PLCGroup()
        {
        }

        public PLCGroup(short id, string name, int updateRate, bool active, IPLCDriver plcReader)
        {
            this._id = id;
            this._name = name;
            this._updateRate = updateRate;
            this._isActive = active;
            this._plcReader = plcReader;
            this._server = _plcReader.Parent;
            this._changedList = new List<int>();
            this._cacheReader = new ByteCacheReader();
            this._timer = new Timer();
        }

        /// <summary>
        /// 通过传入的TagMetaData数据 来添加为 Itag
        /// </summary>从TagMetaData 列表 到  Itag 列表
        /// <param name="items"></param>
        /// <returns></returns>
        public bool AddItems(IList<TagMetaData> items)
        {
            int count = items.Count;
            if (_items == null) _items = new List<ITag>(count);
            lock (_server.SyncRoot)
            {
                for (int i = 0; i < count; i++)
                {
                    ITag dataItem = null;
                    TagMetaData meta = items[i];
                    if (meta.GroupID == this._id)
                    {
                        DeviceAddress addr = _plcReader.GetDeviceAddress(meta.Address);
                        if (addr.DataSize == 0) addr.DataSize = meta.Size;//如果字节长度为零的，置为TagMetaData的数据长度
                        if (addr.VarType == DataType.NONE) addr.VarType = meta.DataType;//如果是空的类型，置为TagMetaData的数据类型
                        if (addr.VarType != DataType.BOOL) addr.Bit = 0;//如果不是bool ，位操作这里置 0
                        switch (meta.DataType)
                        {
                            case DataType.BOOL:
                                dataItem = new BoolTag(meta.ID, addr, this);
                                break;
                            case DataType.BYTE:
                                dataItem = new ByteTag(meta.ID, addr, this);
                                break;
                            case DataType.WORD:
                                dataItem = new UShortTag(meta.ID, addr, this);
                                break;
                            case DataType.SHORT:
                                dataItem = new ShortTag(meta.ID, addr, this);
                                break;
                            case DataType.DWORD:
                                dataItem = new UIntTag(meta.ID, addr, this);
                                break;
                            case DataType.INT:
                                dataItem = new IntTag(meta.ID, addr, this);
                                break;
                            case DataType.FLOAT:
                                dataItem = new FloatTag(meta.ID, addr, this);
                                break;
                            case DataType.STR:
                                dataItem = new StringTag(meta.ID, addr, this);
                                break;
                        }
                        if (dataItem != null)
                        {
                            //dataItem.Active = meta.Active;
                            dataItem.Description = meta.Description;
                            _items.Add(dataItem);
                            _server.AddItemIndex(meta.Name, dataItem);
                        }
                    }
                }
            }
            _items.TrimExcess();
            _items.Sort();
            UpdatePDUArea();
            return true;
        }

        /// <summary>
        /// 这个是 Itag 的数据来源 
        /// </summary>
        /// <param name="tags"></param>
        /// <returns></returns>
        public bool AddTags(IEnumerable<ITag> tags)
        {
            if (_items == null)
            {
                _items = new List<ITag>();
            }
            foreach (ITag tag in tags)
            {
                if (tag != null)
                {
                    _items.Add(tag);
                }
            }
            _items.TrimExcess();
            _items.Sort();
            UpdatePDUArea();
            return true;
        }

        public bool RemoveItems(params ITag[] items)
        {
            foreach (var item in items)
            {
                _server.RemoveItemIndex(item.GetTagName());
                _items.Remove(item);
            }
            UpdatePDUArea();
            return true;
        }

        protected void UpdatePDUArea()
        {
            int count = _items.Count;
            if (count > 0) 
            {
                DeviceAddress _start = _items[0].Address;
                _start.Bit = 0;//代表不要左移，？？读取全部字节
                int bitCount = _cacheReader.ByteCount;//这个地方是 返回  1，代表一次读取 1 个字节 bitCount
                if (count > 1)
                {
                    int cacheLength = 0;//缓冲区的大小
                    int cacheIndexStart = 0;//缓冲区开始得索引号
                    int startIndex = 0;//开始的索引号
                    DeviceAddress segmentEnd = DeviceAddress.Empty;
                    DeviceAddress tagAddress = DeviceAddress.Empty;
                    DeviceAddress segmentStart = _start;
                    for (int j = 1, i = 1; i < count; i++, j++)
                    {
                        tagAddress = _items[i].Address;//当前变量地址 ，其实已经是第二个
                        int offset1 = _cacheReader.GetOffset(tagAddress, segmentStart);//第一个 第二个 两个起始(Start)位置相减，得到 偏移量 offset1
                        if (offset1 > (_plcReader.PDU - tagAddress.DataSize) / bitCount)//？？代表的估计是最后一个Itag
                        {
                            segmentEnd = _items[i - 1].Address;
                            int len = _cacheReader.GetOffset(segmentEnd, segmentStart);//
                            len += segmentEnd.DataSize <= bitCount ? 1 : segmentEnd.DataSize / bitCount;
                            tagAddress.CacheIndex = (ushort)(cacheIndexStart + len);//更新在缓存中的字节索引
                            _items[i].Address = tagAddress;
                            _rangeList.Add(new PDUArea(segmentStart, len, startIndex, j));//就是一个新的区域，开始的DeviceAddress，和长度,和关联的Itag索引号，以及这一段内存地址包含的Itag个数
                            startIndex += j; j = 0;//j为什么每次 赋值之后，要置为0，因为赋值之后，代表另外一个新计数的开始
                            cacheLength += len;//更新缓存长度
                            cacheIndexStart = cacheLength;
                            segmentStart = tagAddress;//更新数据片段的起始地址
                        }
                        else
                        {
                            tagAddress.CacheIndex = (ushort)(cacheIndexStart + offset1);//就是更新Itag在内存缓存中的字节索引号
                            _items[i].Address = tagAddress;
                        }
                        if (i == count - 1)
                        {
                            segmentEnd = _items[i].Address;
                            int segmentLength = _cacheReader.GetOffset(segmentEnd, segmentStart);
                            if (segmentLength > (_plcReader.PDU - segmentEnd.DataSize) / bitCount)
                            {
                                segmentEnd = _items[i - 1].Address;
                                segmentLength = segmentEnd.DataSize <= bitCount ? 1 : segmentEnd.DataSize / bitCount;
                            }
                            tagAddress.CacheIndex = (ushort)(cacheIndexStart + segmentLength);
                            _items[i].Address = tagAddress;
                            segmentLength += segmentEnd.DataSize <= bitCount ? 1 : segmentEnd.DataSize / bitCount;
                            _rangeList.Add(new PDUArea(segmentStart, segmentLength, startIndex, j + 1));
                            cacheLength += segmentLength;
                        }
                    }
                    _cacheReader.Size = cacheLength;
                }
                else
                {
                    var size= _start.DataSize <= bitCount ? 1 : _start.DataSize / bitCount;
                    _rangeList.Add(new PDUArea(_start, size, 0, 1));
                    _cacheReader.Size = size;//改变Cache的Size属性值将创建Cache的内存区域
                }
            }
        }

        public ITag FindItemByAddress(DeviceAddress addr)
        {
            int index = _items.BinarySearch(new BoolTag(0, addr, null));
            return index < 0 ? null : _items[index];
        }

        public bool SetActiveState(bool active, params short[] items)
        {
            return false;
        }

        object sync = new object();
        protected void timer_Timer(object sender, EventArgs e)
        {
            if (_isActive)
            {
                lock (sync)
                {
                    _changedList.Clear();
                    Poll();
                    if (_changedList.Count > 0)
                        Update();
                }
            }
            else
                return;
        }

        protected virtual void Poll()
        {
            if (_plcReader.IsClosed) return;
            byte[] cache = (byte[])_cacheReader.Cache;//这个是缓存里面的数据，用来和新取得数据进行对比
            int offset = 0;//收到的新的字节中的偏移量
            foreach (PDUArea area in _rangeList)
            {
                byte[] rcvBytes = _plcReader.ReadBytes(area.Start, (ushort)area.Len);//逐一从PLC读取数据读取指定的长度，对于我们的程序就是固定一个长度就好                
                if (rcvBytes == null)
                {
                    //_plcReader.Connect();
                    continue;
                }
                else
                {
                    int index = area.StartIndex;//index指向_items中的Tag元数据  index 是Itag的索引 
                    int count = index + area.Count;//count 是总共的Itag 列表的个数，Count 指单次读取的字节中所包含的 Itag个数
                    while (index < count)
                    {
                        DeviceAddress addr = _items[index].Address;//根据在Itag里面的索引，得到内存中的地址
                        int iByte = addr.CacheIndex;
                        int iByte1 = iByte - offset;//在内存中，因为前一个PDUArea块的存在，偏移量，所以到读取的新的字节数组中，要求减去这个偏移量
                        if (addr.VarType == DataType.BOOL)
                        {
                            int tmp = rcvBytes[iByte1] ^ cache[iByte];//对比一下内存里面的值，看有没有发生变化，有的话就需要更新，保留异或位
                            DeviceAddress next = addr;
                            if (tmp != 0)
                            {
                                while (addr.Start == next.Start)//下面while循环比较同一个字节里面不同的位，是否都发生了变化，因为起始地址都是按字节计算的
                                {
                                    if ((tmp & (1 << next.Bit)) > 0) _changedList.Add(index);//tmp里面记录了所有发生变化的位，后面的Itag要是起始地址相同，说明是一个字节里面不同的位有变化
                                    if (++index < count)
                                        next = _items[index].Address;
                                    else
                                        break;
                                }
                            }
                            else
                            {
                                while (addr.Start == next.Start && ++index < count)
                                {
                                    next = _items[index].Address;
                                }
                            }
                        }
                        else
                        {
                            ushort size = addr.DataSize;//把单个Itag字节长度取出来，一个一个字节进行对比
                            for (int i = 0; i < size; i++)
                            {
                                if (iByte1 + i < rcvBytes.Length && rcvBytes[iByte1 + i] != cache[iByte + i])//只要发现长度size当中有任何变化的值，就是值有变化
                                {
                                    _changedList.Add(index);
                                    break;
                                }
                            }
                            index++;
                        }
                    }
                    for (int j = 0; j < rcvBytes.Length; j++)
                        cache[j + offset] = rcvBytes[j];//将PLC读取的数据写入到CacheReader中，所有都更新到内存中，但只推送那些变化的值
                }
                offset += rcvBytes.Length;
            }
        }

        protected void Update()
        {
            DateTime dt = DateTime.Now;
            if (DataChange != null)
            {
                HistoryData[] values = new HistoryData[_changedList.Count];
                int i = 0;
                foreach (int index in _changedList)
                {
                    ITag item = _items[index];
                    var itemData = item.Read();
                    if (item.Active)
                        item.Update(itemData, dt, QUALITIES.QUALITY_GOOD);
                    /*
                     下面的语言判断是否要更新有变化的Itag:
                     1.对敏感度为零的，只要有任何变化都要更新；？？浮点数和零 比较大小
                     2.对于浮点数，新值的变化要超过敏感的 百分比
                     */
                    if (_deadband == 0 || (item.Address.VarType == DataType.FLOAT &&
                        (Math.Abs(itemData.Single / item.Value.Single - 1) > _deadband)))
                    {
                        values[i].ID = item.ID;
                        values[i].Quality = item.Quality;
                        values[i].Value = itemData;
                        values[i].TimeStamp = item.TimeStamp;
                        i++;
                    }
                }

                foreach (DataChangeEventHandler deleg in DataChange.GetInvocationList())//批量调用绑定在事件上面的委托
                {
                    deleg.BeginInvoke(this, new DataChangeEventArgs(1, values), null, null);
                }
            }
            else
            {
                foreach (int index in _changedList)
                {
                    ITag item = _items[index];
                    if (item.Active)
                        item.Update(item.Read(), dt, QUALITIES.QUALITY_GOOD);
                }
            }
        }

        public void Dispose()
        {
            if (_timer != null)
                _timer.Dispose();
            //if (_items != null)
            //    _items.Clear();
        }

        public virtual HistoryData[] BatchRead(DataSource source, bool isSync, params ITag[] itemArray)
        {
            int len = itemArray.Length;
            HistoryData[] values = new HistoryData[len];
            if (source == DataSource.Device)
            {
                IMultiReadWrite multi = _plcReader as IMultiReadWrite;
                if (multi != null)
                {
                    Array.Sort(itemArray);
                    var itemArr = multi.ReadMultiple(Array.ConvertAll(itemArray, x => x.Address));
                    for (int i = 0; i < len; i++)
                    {
                        values[i].ID = itemArray[i].ID;
                        values[i].Value = itemArr[i].Value;
                        values[i].TimeStamp = itemArr[i].TimeStamp.ToDateTime();
                        itemArray[i].Update(itemArr[i].Value, values[i].TimeStamp, itemArr[i].Quality);
                    }
                }
                else
                {
                    for (int i = 0; i < len; i++)
                    {
                        itemArray[i].Refresh(source);
                        values[i].ID = itemArray[i].ID;
                        values[i].Value = itemArray[i].Value;
                        values[i].Quality = itemArray[i].Quality;
                        values[i].TimeStamp = itemArray[i].TimeStamp;
                    }
                }
            }
            else
            {
                for (int i = 0; i < len; i++)
                {
                    values[i].ID = itemArray[i].ID;
                    values[i].Value = itemArray[i].Value;
                    values[i].Quality = itemArray[i].Quality;
                    values[i].TimeStamp = itemArray[i].TimeStamp;
                }
            }
            return values;
        }

        public virtual int BatchWrite(SortedDictionary<ITag, object> items, bool isSync = true)
        {
            int len = items.Count;
            int rev = 0;
            IMultiReadWrite multi = _plcReader as IMultiReadWrite;
            if (multi != null)
            {
                DeviceAddress[] addrs = new DeviceAddress[len];
                object[] objs = new object[len];
                int i = 0;
                foreach (var item in items)
                {
                    addrs[i] = item.Key.Address;
                    objs[i] = item.Value;
                    i++;
                }
                rev = multi.WriteMultiple(addrs, objs);
            }
            else
            {
                foreach (var tag in items)
                {
                    if (tag.Key.Write(tag.Value) < 0)
                        rev = -1;
                }
            }
            if (DataChange != null && rev >= 0)
            {
                HistoryData[] data = new HistoryData[len];
                int i = 0;
                foreach (var item in items)
                {
                    ITag tag = item.Key;
                    data[i].ID = tag.ID;
                    data[i].TimeStamp = tag.TimeStamp;
                    data[i].Quality = tag.Quality;
                    data[i].Value = item.Key.ToStorage(item.Value);
                    i++;
                }
                foreach (DataChangeEventHandler deleg in DataChange.GetInvocationList())
                {
                    deleg.BeginInvoke(this, new DataChangeEventArgs(1, data), null, null);
                }
            }
            return rev;
        }

        public ItemData<int> ReadInt32(DeviceAddress address, DataSource source = DataSource.Cache)
        {
            return source == DataSource.Cache ? _cacheReader.ReadInt32(address) : _plcReader.ReadInt32(address);
        }

        public ItemData<uint> ReadUInt32(DeviceAddress address, DataSource source = DataSource.Cache)
        {
            return source == DataSource.Cache ? _cacheReader.ReadUInt32(address) : _plcReader.ReadUInt32(address);
        }

        public ItemData<ushort> ReadUInt16(DeviceAddress address, DataSource source = DataSource.Cache)
        {
            return source == DataSource.Cache ? _cacheReader.ReadUInt16(address) : _plcReader.ReadUInt16(address);
        }

        public ItemData<short> ReadInt16(DeviceAddress address, DataSource source = DataSource.Cache)
        {
            return source == DataSource.Cache ? _cacheReader.ReadInt16(address) : _plcReader.ReadInt16(address);
        }

        public ItemData<byte> ReadByte(DeviceAddress address, DataSource source = DataSource.Cache)
        {
            return source == DataSource.Cache ? _cacheReader.ReadByte(address) : _plcReader.ReadByte(address);
        }

        public ItemData<float> ReadFloat(DeviceAddress address, DataSource source = DataSource.Cache)
        {
            return source == DataSource.Cache ? _cacheReader.ReadFloat(address) : _plcReader.ReadFloat(address);
        }

        public ItemData<bool> ReadBool(DeviceAddress address, DataSource source = DataSource.Cache)
        {
            return source == DataSource.Cache ? _cacheReader.ReadBit(address) : _plcReader.ReadBit(address);
        }

        public ItemData<string> ReadString(DeviceAddress address, DataSource source = DataSource.Cache)
        {
            ushort siz = address.DataSize;
            return source == DataSource.Cache ? _cacheReader.ReadString(address, siz) :
                _plcReader.ReadString(address, siz);
        }

        public int WriteInt32(DeviceAddress address, int value)
        {
            int rs = _plcReader.WriteInt32(address, value);
            if (rs >= 0)
            {
                if (DataChange != null)
                {
                    ITag tag = GetTagByAddress(address);
                    if (tag != null)
                        DataChange(this, new DataChangeEventArgs(1, new HistoryData[1]
                {
                    new HistoryData(tag.ID,QUALITIES.QUALITY_GOOD,new Storage{Int32=value}, DateTime.Now)
                }));
                }
            }
            return rs;
        }

        public int WriteUInt32(DeviceAddress address, uint value)
        {
            int rs = _plcReader.WriteUInt32(address, value);
            if (rs >= 0)
            {
                if (DataChange != null)
                {
                    ITag tag = GetTagByAddress(address);
                    if (tag != null)
                        DataChange(this, new DataChangeEventArgs(1, new HistoryData[1]
                {
                    new HistoryData(tag.ID,QUALITIES.QUALITY_GOOD,new Storage{DWord=value}, DateTime.Now)
                }));
                }
            }
            return rs;
        }

        public int WriteUInt16(DeviceAddress address, ushort value)
        {
            int rs = _plcReader.WriteUInt16(address, value);
            if (rs >= 0)
            {
                if (DataChange != null)
                {
                    ITag tag = GetTagByAddress(address);
                    if (tag != null)
                        DataChange(this, new DataChangeEventArgs(1, new HistoryData[1]
                {
                    new HistoryData(tag.ID,QUALITIES.QUALITY_GOOD,new Storage{Word=value}, DateTime.Now)
                }));
                }
            }
            return rs;
        }

        public int WriteInt16(DeviceAddress address, short value)
        {
            int rs = _plcReader.WriteInt16(address, value);
            if (rs >= 0)
            {
                if (DataChange != null)
                {
                    ITag tag = GetTagByAddress(address);
                    if (tag != null)
                        DataChange(this, new DataChangeEventArgs(1, new HistoryData[1]
                {
                    new HistoryData(tag.ID,QUALITIES.QUALITY_GOOD,new Storage{Int16=value}, DateTime.Now)
                }));
                }
            }
            return rs;
        }

        public int WriteFloat(DeviceAddress address, float value)
        {
            int rs = _plcReader.WriteFloat(address, value);
            if (rs >= 0)
            {
                if (DataChange != null)
                {
                    ITag tag = GetTagByAddress(address);
                    if (tag != null)
                        DataChange(this, new DataChangeEventArgs(1, new HistoryData[1]
                {
                    new HistoryData(tag.ID,QUALITIES.QUALITY_GOOD,new Storage{Single=value}, DateTime.Now)
                }));
                }
            }
            return rs;
        }

        public int WriteString(DeviceAddress address, string value)
        {
            int rs = _plcReader.WriteString(address, value);
            if (rs >= 0)
            {
                if (DataChange != null)
                {
                    ITag tag = GetTagByAddress(address);
                    if (tag != null)
                        DataChange(this, new DataChangeEventArgs(1, new HistoryData[1]
                {
                    new HistoryData(tag.ID,QUALITIES.QUALITY_GOOD,Storage.Empty, DateTime.Now)
                }));
                }
            }
            return rs;
        }

        public int WriteBit(DeviceAddress address, bool value)
        {
            int rs = _plcReader.WriteBit(address, value);
            if (rs >= 0)
            {
                if (DataChange != null)
                {
                    ITag tag = GetTagByAddress(address);
                    if (tag != null)
                        DataChange(this, new DataChangeEventArgs(1, new HistoryData[1]
                {
                    new HistoryData(tag.ID,QUALITIES.QUALITY_GOOD,new Storage{Boolean=value}, DateTime.Now)
                }));
                }
            }
            return rs;
        }

        public int WriteBits(DeviceAddress address, byte value)
        {
            int rs = _plcReader.WriteBits(address, value);
            if (rs >= 0)
            {
                if (DataChange != null)
                {
                    ITag tag = GetTagByAddress(address);
                    if (tag != null)
                        DataChange(this, new DataChangeEventArgs(1, new HistoryData[1]
                {
                    new HistoryData(tag.ID,QUALITIES.QUALITY_GOOD,new Storage{Byte=value}, DateTime.Now)
                }));
                }
            }
            return rs;
        }

        private ITag GetTagByAddress(DeviceAddress addr)
        {
            int index = _items.BinarySearch(new BoolTag(0, addr, null));
            return index < 0 ? null : _items[index];
        }

        public event DataChangeEventHandler DataChange;
    }

    /// <summary>
    /// 简单的结构体，重要的只有一个DeviceAddress地址变量
    /// </summary>
    public struct PDUArea
    {
        public DeviceAddress Start;//对应 Itag 在下位机的地址，和底层具体的驱动有关系
        public int Len;//是从Start 这个地址开始，要拷贝的字节的长度 是Len
        public int StartIndex;// 在 Itag 列表中的索引
        public int Count;//？？代表当前地址所包含的Itag的个数

        public PDUArea(DeviceAddress start, int len, int startIndex, int count)
        {
            Start = start;
            Len = len;
            StartIndex = startIndex;
            Count = count;
        }
    }

    public sealed class NetBytePLCGroup : PLCGroup
    {
        public NetBytePLCGroup(short id, string name, int updateRate, bool active, IPLCDriver plcReader)
        {
            this._id = id;
            this._name = name;
            this._updateRate = updateRate;
            this._isActive = active;
            this._plcReader = plcReader;
            this._server = _plcReader.Parent;
            this._timer = new Timer();
            this._changedList = new List<int>();
            this._cacheReader = new NetByteCacheReader();
        }

        protected override void Poll()
        {
            if (_plcReader.IsClosed) return;//如果连接断开，返回不操作
            if (_items == null || _items.Count == 0) return;
            byte[] cache = (byte[])_cacheReader.Cache;
            int offset = 0;
            foreach (PDUArea area in _rangeList)
            {
                byte[] rcvBytes = _plcReader.ReadBytes(area.Start, (ushort)area.Len);//从PLC读取数据                
                if (rcvBytes == null)
                {
                    //_plcReader.Connect();
                    continue;
                }
                else
                {
                    int index = area.StartIndex;//index指向_items中的Tag元数据
                    int count = index + area.Count;
                    while (index < count)
                    {
                        DeviceAddress addr = _items[index].Address;
                        int iByte = addr.CacheIndex;
                        int iByte1 = iByte - offset;
                        if (addr.VarType == DataType.BOOL)
                        {
                            int tmp = rcvBytes[iByte1] ^ cache[iByte];
                            DeviceAddress next = addr;
                            if (tmp != 0)
                            {
                                while (addr.Start == next.Start)
                                {
                                    if ((tmp & (1 << next.Bit)) > 0) _changedList.Add(index);
                                    if (++index < count)
                                        next = _items[index].Address;
                                    else
                                        break;
                                }
                            }
                            else
                            {
                                while (addr.Start == next.Start && ++index < count)
                                {
                                    next = _items[index].Address;
                                }
                            }
                        }
                        else
                        {
                            ushort size = addr.DataSize;
                            for (int i = 0; i < size; i++)
                            {
                                if (iByte1 + i < rcvBytes.Length && rcvBytes[iByte1 + i] != cache[iByte + i])
                                {
                                    _changedList.Add(index);
                                    break;
                                }
                            }
                            index++;
                        }
                    }
                    for (int j = 0; j < rcvBytes.Length; j++)
                        cache[j + offset] = rcvBytes[j];//将PLC读取的数据写入到CacheReader中
                }
                offset += rcvBytes.Length;
            }
        }
    }

    public sealed class NetShortGroup : PLCGroup
    {
        public NetShortGroup(short id, string name, int updateRate, bool active, IPLCDriver plcReader)
        {
            this._id = id;
            this._name = name;
            this._updateRate = updateRate;
            this._isActive = active;
            this._plcReader = plcReader;
            this._server = _plcReader.Parent;
            this._timer = new Timer();
            this._changedList = new List<int>();
            this._cacheReader = new NetShortCacheReader();
        }

        protected override unsafe void Poll()
        {
            short[] cache = (short[])_cacheReader.Cache;
            int offset = 0;
            foreach (PDUArea area in _rangeList)
            {
                byte[] rcvBytes = _plcReader.ReadBytes(area.Start, (ushort)area.Len);//从PLC读取数据  
                if (rcvBytes == null || rcvBytes.Length == 0)
                {
                    offset += (area.Len + 1) / 2;
                    //_plcReader.Connect();
                    continue;
                }
                else
                {
                    int len = rcvBytes.Length / 2;
                    fixed (byte* p1 = rcvBytes)
                    {
                        short* prcv = (short*)p1;
                        int index = area.StartIndex;//index指向_items中的Tag元数据
                        int count = index + area.Count;
                        while (index < count)
                        {
                            DeviceAddress addr = _items[index].Address;
                            int iShort = addr.CacheIndex;
                            int iShort1 = iShort - offset;
                            if (addr.VarType == DataType.BOOL)
                            {
                                if (addr.ByteOrder.HasFlag(ByteOrder.Network)) prcv[iShort1] = IPAddress.HostToNetworkOrder(prcv[iShort1]);
                                int tmp = prcv[iShort1] ^ cache[iShort];
                                DeviceAddress next = addr;
                                if (tmp != 0)
                                {
                                    while (addr.Start == next.Start)
                                    {
                                        if ((tmp & (1 << next.Bit)) > 0) _changedList.Add(index);
                                        if (++index < count)
                                            next = _items[index].Address;
                                        else
                                            break;
                                    }
                                }
                                else
                                {
                                    while (addr.Start == next.Start && ++index < count)
                                    {
                                        next = _items[index].Address;
                                    }
                                }
                            }
                            else
                            {
                                if (addr.DataSize <= 2)
                                {
                                    if (prcv[iShort1] != cache[iShort]) _changedList.Add(index);
                                }
                                else
                                {
                                    int size = addr.DataSize / 2;
                                    for (int i = 0; i < size; i++)
                                    {
                                        if (prcv[iShort1 + i] != cache[iShort + i])
                                        {
                                            _changedList.Add(index);
                                            break;
                                        }
                                    }
                                }
                                index++;
                            }
                        }
                        for (int j = 0; j < len; j++)
                        {
                            cache[j + offset] = prcv[j];
                        }//将PLC读取的数据写入到CacheReader中
                    }
                    offset += len;
                }
            }
        }
    }

    public sealed class ShortGroup : PLCGroup
    {
        public ShortGroup(short id, string name, int updateRate, bool active, IPLCDriver plcReader)
        {
            this._id = id;
            this._name = name;
            this._updateRate = updateRate;
            this._isActive = active;
            this._plcReader = plcReader;
            this._server = _plcReader.Parent;
            this._timer = new Timer();
            this._changedList = new List<int>();
            this._cacheReader = new ShortCacheReader();
        }

        protected override unsafe void Poll()
        {
            short[] cache = (short[])_cacheReader.Cache;
            int k = 0;
            foreach (PDUArea area in _rangeList)
            {
                byte[] rcvBytes = _plcReader.ReadBytes(area.Start, (ushort)area.Len);//从PLC读取数据  
                if (rcvBytes == null)
                {
                    k += (area.Len + 1) / 2;
                    continue;
                }
                else
                {
                    int len = rcvBytes.Length / 2;
                    fixed (byte* p1 = rcvBytes)
                    {
                        short* prcv = (short*)p1;
                        int index = area.StartIndex;//index指向_items中的Tag元数据
                        int count = index + area.Count;
                        while (index < count)
                        {
                            DeviceAddress addr = _items[index].Address;
                            int iShort = addr.CacheIndex;
                            int iShort1 = iShort - k;
                            if (addr.VarType == DataType.BOOL)
                            {
                                int tmp = prcv[iShort1] ^ cache[iShort];
                                DeviceAddress next = addr;
                                if (tmp != 0)
                                {
                                    while (addr.Start == next.Start)
                                    {
                                        if ((tmp & (1 << next.Bit)) > 0) _changedList.Add(index);
                                        if (++index < count)
                                            next = _items[index].Address;
                                        else
                                            break;
                                    }
                                }
                                else
                                {
                                    while (addr.Start == next.Start && ++index < count)
                                    {
                                        next = _items[index].Address;
                                    }
                                }
                            }
                            else
                            {
                                if (addr.ByteOrder.HasFlag(ByteOrder.BigEndian))
                                {
                                    for (int i = 0; i < addr.DataSize / 2; i++)
                                    {
                                        prcv[iShort1 + i] = IPAddress.HostToNetworkOrder(prcv[iShort1 + i]);
                                    }
                                }
                                if (addr.DataSize <= 2)
                                {
                                    if (prcv[iShort1] != cache[iShort]) _changedList.Add(index);
                                }
                                else
                                {
                                    int size = addr.DataSize / 2;
                                    for (int i = 0; i < size; i++)
                                    {
                                        if (prcv[iShort1 + i] != cache[iShort + i])
                                        {
                                            _changedList.Add(index);
                                            break;
                                        }
                                    }
                                }
                                index++;
                            }
                        }
                        for (int j = 0; j < len; j++)
                        {
                            cache[j + k] = prcv[j];
                        }//将PLC读取的数据写入到CacheReader中
                    }
                    k += len;
                }
            }
        }

    }
}
