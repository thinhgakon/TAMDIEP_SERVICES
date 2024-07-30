using S7.Net;
using System;

namespace XHTD_SERVICES.Device.PLCS71200
{
    public abstract class S71200
    {
        protected Plc _plc;

        protected S71200(Plc plc)
        {
            _plc = plc;
        }

        public bool ReadInputPort(string variable)
        {
            try
            {
                var result = _plc.Read(variable);
                if (result is bool outputValue && outputValue)
                {
                    return true;
                }
                else {
                    return false;
                }
            }
            catch (Exception)
            {
                return false;
            }

        }

        public ErrorCode ReadOutputPort(string variable)
        {
            try
            {
                var result = _plc.Read(variable);
                if (result is bool outputValue && outputValue)
                {
                    return ErrorCode.NoError;
                }
                return ErrorCode.ReadData;
            }
            catch (Exception)
            {
                return ErrorCode.ReadData;
            }

        }

        public ErrorCode ShuttleOutputPort(string variable, bool value)
        {
            try
            {
                _plc.Write(variable, value);
                return ErrorCode.NoError;
            }
            catch (Exception)
            {
                return ErrorCode.WriteData;
            }
        }

        public ErrorCode Open()
        {
            try
            {
                _plc.Open();
                return ErrorCode.NoError;
            }
            catch (Exception)
            {
                return ErrorCode.ConnectionError;
            }
        }

        public ErrorCode Close()
        {
            try
            {
                _plc.Close();
                return ErrorCode.NoError;
            }
            catch (Exception)
            {
                return ErrorCode.ConnectionError;
            }
        }

        public bool IsConnected => this._plc.IsConnected;
    }
}
