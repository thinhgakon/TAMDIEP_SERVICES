using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace XHTD_SERVICES_LED.Libs
{
    class CSDKExport
    {
        // Get last error
        [DllImport("HDSDK.dll", EntryPoint = "Hd_GetSDKLastError", CallingConvention = CallingConvention.StdCall)]
        public static extern int Hd_GetSDKLastError();

        // Create screen
        [DllImport("HDSDK.dll", EntryPoint = "Hd_CreateScreen", CallingConvention = CallingConvention.StdCall)]
        public static extern int Hd_CreateScreen(int nWidth, int nHeight, int nColor, int nGray, int nCardType, IntPtr pExParamsBuf, int nBufSize);

        // Add program 
        [DllImport("HDSDK.dll", EntryPoint = "Hd_AddProgram", CallingConvention = CallingConvention.StdCall)]
        public static extern int Hd_AddProgram(IntPtr pBoderImgPath, int nBorderEffect, int nBorderSpeed, IntPtr pExParamsBuf, int nBufSize);

        // Add area
        [DllImport("HDSDK.dll", EntryPoint = "Hd_AddArea", CallingConvention = CallingConvention.StdCall)]
        public static extern int Hd_AddArea(int nProgramID, int nX, int nY, int nWidth, int nHeight, IntPtr pBoderImgPath, int nBorderEffect, int nBorderSpeed, IntPtr pExParamsBuf, int nBufSize);

        // Add image area item
        [DllImport("HDSDK.dll", EntryPoint = "Hd_AddImageAreaItem", CallingConvention = CallingConvention.StdCall)]
        public static extern int Hd_AddImageAreaItem(int nAreaID, IntPtr pPaths, int nShowEffect, int nShowSpeed, int nClearType, int nStayTime, IntPtr pExParamsBuf, int nBufSize);

        // Add text area item
        [DllImport("HDSDK.dll", EntryPoint = "Hd_AddSimpleTextAreaItem", CallingConvention = CallingConvention.StdCall)]
        public static extern int Hd_AddSimpleTextAreaItem(int nAreaID, IntPtr pText, int nTextColor, int nBackGroupColor, int nStyle, IntPtr pFontName, int nFontHeight, int nShowEffect, int nShowSpeed, int nClearType, int nStayTime, IntPtr pExParamsBuf, int nBufSize);

        // Add time area item
        [DllImport("HDSDK.dll", EntryPoint = "Hd_AddTimeAreaItem", CallingConvention = CallingConvention.StdCall)]
        public static extern int Hd_AddTimeAreaItem(int nAreaID, int nShowMode, int bShowDate, int nDateStyle, int bShowWeek, int nWeekStyle, int bShowTime, int nTimeStyle, int nTextColor, IntPtr pFontName, int nFontHeight, int nDiffHour, int nDiffMin, IntPtr pExParamsBuf, int nBufSize);

        // Create realtime area item
        [DllImport("HDSDK.dll", EntryPoint = "Hd_CreateRealtimeArea", CallingConvention = CallingConvention.StdCall)]
        public static extern int Hd_CreateRealtimeArea(int nX, int nY, int nWidth, int nHeight, IntPtr pImgPath, int nUseRealTime, int bOnlyShowRealtimeArea, int bSave, int nLivetime, IntPtr pExParamsBuf, int nBufSize);

        // Send command
        [DllImport("HDSDK.dll", EntryPoint = "Hd_SendCommand", CallingConvention = CallingConvention.StdCall)]
        public static extern int Hd_SendCommand(int nSendType, IntPtr pStrParams, int nCommandType, IntPtr pConText, int nTextLen, IntPtr pDeviceGUID, IntPtr pOutConText, ref int pOutConTextLen, IntPtr pExParamsBuf, int nBufSize);

        // Send screen
        [DllImport("HDSDK.dll", EntryPoint = "Hd_SendScreen", CallingConvention = CallingConvention.StdCall)]
        public static extern int Hd_SendScreen(int nSendType, IntPtr pStrParams, IntPtr pDeviceGUID, IntPtr pExParamsBuf, int nBufSize);

        // Send realtime area
        [DllImport("HDSDK.dll", EntryPoint = "Hd_SendRealTimeArea", CallingConvention = CallingConvention.StdCall)]
        public static extern int Hd_SendRealTimeArea(int nSendType, IntPtr pStrParams, IntPtr pDeviceGUID, IntPtr pExParamsBuf, int nBufSize);

        // Get Color value
        [DllImport("HDSDK.dll", EntryPoint = "Hd_GetColor", CallingConvention = CallingConvention.StdCall)]
        public static extern int Hd_GetColor(int r, int g, int b);

        // Save screen to disk
        [DllImport("HDSDK.dll", EntryPoint = "Hd_SaveScreen", CallingConvention = CallingConvention.StdCall)]
        public static extern int Hd_SaveScreen(IntPtr pDirPath);

        // command 
        // Get baudrate
        [DllImport("HDSDK.dll", EntryPoint = "Cmd_GetBaudRate", CallingConvention = CallingConvention.StdCall)]
        public static extern int Cmd_GetBaudRate(int nPort, ref int pBaudRate, IntPtr pDeviceGUID);

        // Set baudrate
        [DllImport("HDSDK.dll", EntryPoint = "Cmd_SetBaudRate", CallingConvention = CallingConvention.StdCall)]
        public static extern int Cmd_SetBaudRate(int nPort, int nSrcBaudRate, int nDestBaudRate, IntPtr pDeviceGUID);

        // Is card online
        [DllImport("HDSDK.dll", EntryPoint = "Cmd_IsCardOnline", CallingConvention = CallingConvention.StdCall)]
        public static extern int Cmd_IsCardOnline(int nSendType, IntPtr pStrParams, IntPtr pDeviceGUID);

        // Clear screen
        [DllImport("HDSDK.dll", EntryPoint = "Cmd_ClearScreen", CallingConvention = CallingConvention.StdCall)]
        public static extern int Cmd_ClearScreen(int nSendType, IntPtr pStrParams, IntPtr pDeviceGUID);

        // Restart card
        [DllImport("HDSDK.dll", EntryPoint = "Cmd_RestartCard", CallingConvention = CallingConvention.StdCall)]
        public static extern int Cmd_RestartCard(int nSendType, IntPtr pStrParams, IntPtr pDeviceGUID);

        // Test screen
        [DllImport("HDSDK.dll", EntryPoint = "Cmd_TestScreen", CallingConvention = CallingConvention.StdCall)]
        public static extern int Cmd_TestScreen(int nSendType, IntPtr pStrParams, int nStyle, IntPtr pDeviceGUID);

        // adjust time
        [DllImport("HDSDK.dll", EntryPoint = "Cmd_AdjustTime", CallingConvention = CallingConvention.StdCall)]
        public static extern int Cmd_AdjustTime(int nSendType, IntPtr pStrParams, IntPtr pDeviceGUID);

        // set luminance
        [DllImport("HDSDK.dll", EntryPoint = "Cmd_SetLuminance", CallingConvention = CallingConvention.StdCall)]
        public static extern int Cmd_SetLuminance(int nSendType, IntPtr pStrParams, int nLuminance, IntPtr pDeviceGUID);

        // screen ctrl
        [DllImport("HDSDK.dll", EntryPoint = "Cmd_ScreenCtrl", CallingConvention = CallingConvention.StdCall)]
        public static extern int Cmd_ScreenCtrl(int nSendType, IntPtr pStrParams, int nCtrlCode, IntPtr pDeviceGUID);

        // timeswitch
        [DllImport("HDSDK.dll", EntryPoint = "Cmd_TimeSwitch", CallingConvention = CallingConvention.StdCall)]
        public static extern int Cmd_TimeSwitch(int nSendType, IntPtr pStrParams, int nUse, int nBootuUpTime, int nShutDownTime, IntPtr pDeviceGUID);

        // switch program
        [DllImport("HDSDK.dll", EntryPoint = "Cmd_SwitchProgram", CallingConvention = CallingConvention.StdCall)]
        public static extern int Cmd_SwitchProgram(int nSendType, IntPtr pStrParams, int nProgramIndex, IntPtr pDeviceGUID);

        // Set ip
        [DllImport("HDSDK.dll", EntryPoint = "Cmd_SetIP", CallingConvention = CallingConvention.StdCall)]
        public static extern int Cmd_SetIP(IntPtr pSrcIP, IntPtr pDestIP, IntPtr pDestMask, IntPtr pDestGateway, IntPtr pDeviceGUID);

        // send realtime screen
        [DllImport("HDSDK.dll", EntryPoint = "Hd_Rt_SendScreen", CallingConvention = CallingConvention.StdCall)]
        public static extern int Hd_Rt_SendScreen(int nSendType, IntPtr pStrParams, IntPtr pDeviceGUID, IntPtr pExParamsBuf, int nBufSize);

        // add realtimearea
        [DllImport("HDSDK.dll", EntryPoint = "Hd_Rt_AddRealAreaToScreen", CallingConvention = CallingConvention.StdCall)]
        public static extern int Hd_Rt_AddRealAreaToScreen(int nX, int nY, int nWidth, int nHeight, int nMaxPageCount);

        // send realtime text
        [DllImport("HDSDK.dll", EntryPoint = "Hd_Rt_SendRealTimeText", CallingConvention = CallingConvention.StdCall)]
        public static extern int Hd_Rt_SendRealTimeText(int nSendType, IntPtr pStrParams, int nRealTimeAreaIndex, int nMaxPage, int nColor, int nGray, int nX, int nY, int nWidth, int nHeight,
            IntPtr pText, int nTextColor, int nBackGroupColor, int nStyle, IntPtr pFontName, int nFontHeight, int nShowEffect, int nShowSpeed,
            int nStayTime, int nLiveTime, int bSaveToFlash, IntPtr pDeviceGUID);

        // send realtime image
        [DllImport("HDSDK.dll", EntryPoint = "Hd_Rt_SendRealTimeImage", CallingConvention = CallingConvention.StdCall)]
        public static extern int Hd_Rt_SendRealTimeImage(int nSendType, IntPtr pStrParams, int nRealTimeAreaIndex, int nMaxPage, int nColor, int nGray, int nX, int nY, int nWidth, int nHeight,
            IntPtr pPaths, int nShowEffect, int nShowSpeed, int nStayTime, int nLiveTime, int bSaveToFlash, IntPtr pDeviceGUID);

        // set screen params
        [DllImport("HDSDK.dll", EntryPoint = "Hd_SetScreenParam", CallingConvention = CallingConvention.StdCall)]
        public static extern int Hd_SetScreenParam(int bUseTimerSwitch, int nBootuUpTime, int nShutDownTime, int nBrightnessMode, int nCustomBrightValue);

        // set program params
        [DllImport("HDSDK.dll", EntryPoint = "Hd_SetProgramParam", CallingConvention = CallingConvention.StdCall)]
        public static extern int Hd_SetProgramParam(int nProgramID, int nPlayMode, int nPlayLength, IntPtr pBorderPath,
            int nBordernSpeed, int nBordernEffect, int nWeekPlayFlag, int nSpecifedTimeEnabled,
            int nTimeStart, int nTimeEnd, int nSpecifedDateEnabled, int nDateStart, int nDateEnd);

        // add Chinese calendar area item
        [DllImport("HDSDK.dll", EntryPoint = "Hd_AddChineseCalendarAreaItem", CallingConvention = CallingConvention.StdCall)]
        public static extern int Hd_AddChineseCalendarAreaItem(int nAreaID, int nLanguage, int nShowType, int bShowHeavenly, int nHeavenlyColor, int bShowCalendar,
            int nCalendarColor, int bShowSolarTerms, int nSolarTermsColor, int bShowFestival, IntPtr pFontName, int nFontHeight, IntPtr pExParamsBuf, int nBufSize);

        // add temperature and humidity area item
        [DllImport("HDSDK.dll", EntryPoint = "Hd_AddTempAndHumiAreaItem", CallingConvention = CallingConvention.StdCall)]
        public static extern int Hd_AddTempAndHumiAreaItem(int nAreaID, int nSensorType, int bUseTemperature, IntPtr pTemperatureText, int nTemperatureTextColor, int nTemperatureX, int nTemperatureY,
            int nTemperatureUnit, int nTemperatureUnitColor, int nTemperatureOffset, int nTemperatureValueColor, int bUseHumidity, IntPtr pHumidityText, int nHumidityTextColor, int nHumidityX, int nHumidityY,
            int nHumidityUnit, int nHumidityUnitColor, int nHumidityOffset, int nHumidityValueColor,
            IntPtr pFontName, int nFontHeight, IntPtr pExParamsBuf, int nBufSize);

        // add count area item
        [DllImport("HDSDK.dll", EntryPoint = "Hd_AddCountAreaItem", CallingConvention = CallingConvention.StdCall)]
        public static extern int Hd_AddCountAreaItem(int nAreaID, int nCountMode, int nShowMode, int nAlignment, int nYear, int nMonth, int nDay, int nHour, int nMinute, int nSecond, int nValueColor, int nUnitColor,
            int bShowDay, int bShowHour, int bShowMinute, int bShowSecond, int bShowMS, int bAddUp, int bCycletiming, int nX, int nY, IntPtr pFontName, int nFontHeight, IntPtr pExParamsBuf, int nBufSize);

        // add digit area item
        [DllImport("HDSDK.dll", EntryPoint = "Hd_AddDigitAreaItem", CallingConvention = CallingConvention.StdCall)]
        public static extern int Hd_AddDigitAreaItem(int nAreaID, int nInitialState, int nAlignment, int nMin, int nMinDecimalValue, int nMax, int nMaxDecimalValue, int nDecimalDigit, int nColor,
            int nTransitionTreshold, int nTransitionTresholdDecimalValue, int bUseMinImage, IntPtr pMinImagePath, int bUseMaxImage, IntPtr pMaxImagePath, int bSaveDigit, int nX, int nY, IntPtr pFontName, int nFontHeight, IntPtr pExParamsBuf, int nBufSize);

        // clear realtime area
        [DllImport("HDSDK.dll", EntryPoint = "Cmd_ClearRealtimeArea", CallingConvention = CallingConvention.StdCall)]
        public static extern int Cmd_ClearRealtimeArea(int nSendType, IntPtr pStrParams, IntPtr pDeviceGUID);

        // set digit state
        [DllImport("HDSDK.dll", EntryPoint = "Cmd_SetDigitState", CallingConvention = CallingConvention.StdCall)]
        public static extern int Cmd_SetDigitState(int nSendType, IntPtr pStrParams, int nAreaIndex, int nCmdType, int nValue, int nDecimaValue, IntPtr pDeviceGUID);

        // set count state
        [DllImport("HDSDK.dll", EntryPoint = "Cmd_SetCountState", CallingConvention = CallingConvention.StdCall)]
        public static extern int Cmd_SetCountState(int nSendType, IntPtr pStrParams, int nAreaIndex, int nCmdType, IntPtr pDeviceGUID);
    }
}
