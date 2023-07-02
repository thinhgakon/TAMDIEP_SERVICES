using Quartz;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using XHTD_SERVICES_LED.Libs;
using XHTD_SERVICES_LED.Models.Response;
using XHTD_SERVICES.Data.Repositories;
using XHTD_SERVICES.Helper;

namespace XHTD_SERVICES_LED.Jobs
{
    public class LedXiRoi : IJob
    {
        int m_nSendType_LED12;
        IntPtr m_pSendParams_LED12;
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger
      (System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        protected readonly StoreOrderOperatingRepository _storeOrderOperatingRepository;

        public LedXiRoi(StoreOrderOperatingRepository storeOrderOperatingRepository)
        {
            _storeOrderOperatingRepository = storeOrderOperatingRepository;
            m_nSendType_LED12 = 0;
            string strParams_LED12 = "10.0.7.2";
            m_pSendParams_LED12 = Marshal.StringToHGlobalUni(strParams_LED12);
        }
        public async Task Execute(IJobExecutionContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }
            await Task.Run(() =>
            {
                ShowLed12Process();
            });
        }
        public async void ShowLed12Process()
        {
            try { 
                log.Info("start show led roi");
                Console.WriteLine("start show led roi");

                // đổ data thay thế vào đây
                var orderShows = new List<StoreOrderForLED12>();
                var orders = await _storeOrderOperatingRepository.GetOrdersLedXiRoi();

                if (orders == null || orders.Count == 0)
                {
                    SetLED12NoContent();
                    return;
                }

                foreach (var order in orders)
                {
                    orderShows.Add(new StoreOrderForLED12 { Vehicle = LedHelper.GetDisplayVehicle(order.Vehicle), State1 = LedHelper.GetDisplayMachine(order.Machine) }); //1
                }

                //nếu không có data thì sử dụng màn hình led với thông tin mong muốn ở hàm  SetLED12NoContent

                //var orderShows = new List<StoreOrderForLED12>();
                //orderShows.Add(new StoreOrderForLED12 { Vehicle = "15H090", State1 = "M9" }); //1
                //orderShows.Add(new StoreOrderForLED12 { Vehicle = "15H010129", State1 = "M9" }); //2
                //orderShows.Add(new StoreOrderForLED12 { Vehicle = "15H01013", State1 = "M9" }); //3
                //orderShows.Add(new StoreOrderForLED12 { Vehicle = "15H01014", State1 = "M9" }); //4
                //orderShows.Add(new StoreOrderForLED12 { Vehicle = "15H01015", State1 = "M10" }); //5
                //orderShows.Add(new StoreOrderForLED12 { Vehicle = "15H01016", State1 = "M10" }); //6
                //orderShows.Add(new StoreOrderForLED12 { Vehicle = "15H01017", State1 = "M10" }); //7
                //orderShows.Add(new StoreOrderForLED12 { Vehicle = "15H01018", State1 = "M10" }); //8
                //orderShows.Add(new StoreOrderForLED12 { Vehicle = "15H01019", State1 = "M10" }); //9

                IntPtr pNULL = new IntPtr(0);

                int nErrorCode = -1;
                // 1. Create a screen
                int nWidth = 160;
                int nHeight = 112;
                int nColor = 1;
                int nGray = 1;
                int nCardType = 0;

                int nRe = CSDKExport.Hd_CreateScreen(nWidth, nHeight, nColor, nGray, nCardType, pNULL, 0);
                if (nRe != 0)
                {
                    nErrorCode = CSDKExport.Hd_GetSDKLastError();
                    return;
                }

                // 2. Add program to screen
                int nProgramID = CSDKExport.Hd_AddProgram(pNULL, 0, 0, pNULL, 0);
                if (nProgramID == -1)
                {
                    nErrorCode = CSDKExport.Hd_GetSDKLastError();
                    return;
                }

                int plusX = 6;
                int plusY = 6;

                int WIDTH_LEFT = 100;
                int WIDTH_RIGHT = 60;

                int HEIGHT_ROW = 16;

                int nAreaWidth = WIDTH_LEFT - plusX;
                int nAreaHeight = HEIGHT_ROW;

                int nAreaWidth_2 = WIDTH_RIGHT - plusX;
                int nAreaHeight_2 = HEIGHT_ROW;

                #region Add Area 0
                int nX1 = 0 + plusX;
                int nY1 = 0 * HEIGHT_ROW + plusY;

                int nAreaID_1 = CSDKExport.Hd_AddArea(nProgramID, nX1, nY1, nAreaWidth, nAreaHeight, pNULL, 0, 0, pNULL, 0);
                if (nAreaID_1 == -1)
                {
                    nErrorCode = CSDKExport.Hd_GetSDKLastError();
                    return;
                }
                #endregion

                #region Add Area 0_2
                int nX1_2 = WIDTH_LEFT + plusX;
                int nY1_2 = 0 * HEIGHT_ROW + plusY;

                int nAreaID_1_2 = CSDKExport.Hd_AddArea(nProgramID, nX1_2, nY1_2, nAreaWidth_2, nAreaHeight_2, pNULL, 0, 0, pNULL, 0);
                if (nAreaID_1_2 == -1)
                {
                    nErrorCode = CSDKExport.Hd_GetSDKLastError();
                    return;
                }
                #endregion

                #region Add Area 1
                int nX2 = 0 + plusX;
                int nY2 = 1 * HEIGHT_ROW + plusY;

                int nAreaID_2 = CSDKExport.Hd_AddArea(nProgramID, nX2, nY2, nAreaWidth, nAreaHeight, pNULL, 0, 0, pNULL, 0);
                if (nAreaID_2 == -1)
                {
                    nErrorCode = CSDKExport.Hd_GetSDKLastError();
                    return;
                }
                #endregion

                #region Add Area 1_2
                int nX2_2 = WIDTH_LEFT + plusX;
                int nY2_2 = 1 * HEIGHT_ROW + plusY;

                int nAreaID_2_2 = CSDKExport.Hd_AddArea(nProgramID, nX2_2, nY2_2, nAreaWidth_2, nAreaHeight_2, pNULL, 0, 0, pNULL, 0);
                if (nAreaID_2_2 == -1)
                {
                    nErrorCode = CSDKExport.Hd_GetSDKLastError();
                    return;
                }
                #endregion

                #region Add Area 2
                int nX3 = 0 + plusX;
                int nY3 = 2 * HEIGHT_ROW + plusY;

                int nAreaID_3 = CSDKExport.Hd_AddArea(nProgramID, nX3, nY3, nAreaWidth, nAreaHeight, pNULL, 0, 0, pNULL, 0);
                if (nAreaID_3 == -1)
                {
                    nErrorCode = CSDKExport.Hd_GetSDKLastError();
                    return;
                }
                #endregion

                #region Add Area 2_2
                int nX3_2 = WIDTH_LEFT + plusX;
                int nY3_2 = 2 * HEIGHT_ROW + plusY;

                int nAreaID_3_2 = CSDKExport.Hd_AddArea(nProgramID, nX3_2, nY3_2, nAreaWidth_2, nAreaHeight_2, pNULL, 0, 0, pNULL, 0);
                if (nAreaID_3_2 == -1)
                {
                    nErrorCode = CSDKExport.Hd_GetSDKLastError();
                    return;
                }
                #endregion

                #region Add Area 3
                int nX4 = 0 + plusX;
                int nY4 = 3 * HEIGHT_ROW + plusY;

                int nAreaID_4 = CSDKExport.Hd_AddArea(nProgramID, nX4, nY4, nAreaWidth, nAreaHeight, pNULL, 0, 0, pNULL, 0);
                if (nAreaID_4 == -1)
                {
                    nErrorCode = CSDKExport.Hd_GetSDKLastError();
                    return;
                }
                #endregion

                #region Add Area 3_2
                int nX4_2 = WIDTH_LEFT + plusX;
                int nY4_2 = 3 * HEIGHT_ROW + plusY;

                int nAreaID_4_2 = CSDKExport.Hd_AddArea(nProgramID, nX4_2, nY4_2, nAreaWidth_2, nAreaHeight_2, pNULL, 0, 0, pNULL, 0);
                if (nAreaID_4_2 == -1)
                {
                    nErrorCode = CSDKExport.Hd_GetSDKLastError();
                    return;
                }
                #endregion

                #region Add Area 4
                int nX5 = 0 + plusX;
                int nY5 = 4 * HEIGHT_ROW + plusY;

                int nAreaID_5 = CSDKExport.Hd_AddArea(nProgramID, nX5, nY5, nAreaWidth, nAreaHeight, pNULL, 0, 0, pNULL, 0);
                if (nAreaID_5 == -1)
                {
                    nErrorCode = CSDKExport.Hd_GetSDKLastError();
                    return;
                }
                #endregion

                #region Add Area 4_2
                int nX5_2 = WIDTH_LEFT + plusX;
                int nY5_2 = 4 * HEIGHT_ROW + plusY;

                int nAreaID_5_2 = CSDKExport.Hd_AddArea(nProgramID, nX5_2, nY5_2, nAreaWidth_2, nAreaHeight_2, pNULL, 0, 0, pNULL, 0);
                if (nAreaID_5_2 == -1)
                {
                    nErrorCode = CSDKExport.Hd_GetSDKLastError();
                    return;
                }
                #endregion

                #region Add Area 5
                int nX6 = 0 + plusX;
                int nY6 = 5 * HEIGHT_ROW + plusY;

                int nAreaID_6 = CSDKExport.Hd_AddArea(nProgramID, nX6, nY6, nAreaWidth, nAreaHeight, pNULL, 0, 0, pNULL, 0);
                if (nAreaID_6 == -1)
                {
                    nErrorCode = CSDKExport.Hd_GetSDKLastError();
                    return;
                }
                #endregion

                #region Add Area 5_2
                int nX6_2 = WIDTH_LEFT + plusX;
                int nY6_2 = 5 * HEIGHT_ROW + plusY;

                int nAreaID_6_2 = CSDKExport.Hd_AddArea(nProgramID, nX6_2, nY6_2, nAreaWidth_2, nAreaHeight_2, pNULL, 0, 0, pNULL, 0);
                if (nAreaID_6_2 == -1)
                {
                    nErrorCode = CSDKExport.Hd_GetSDKLastError();
                    return;
                }
                #endregion

                // Config chung
                IntPtr pFontName = Marshal.StringToHGlobalUni("Times New Roman");
                //int nTextColor = CSDKExport.Hd_GetColor(255, 0, 0);
                int nTextColor = CSDKExport.Hd_GetColor(255, 255, 255);
                int nTextStyle = 0x0000 | 0x0100 /*| 0x0200 */;

                #region Show on Area 0: DÒNG TIÊU ĐỀ
                int nFontHeight = 12;
                IntPtr pText = Marshal.StringToHGlobalUni("BIEN SO");
                int nEffect = 0;
                
                int nAreaItemID_1 = CSDKExport.Hd_AddSimpleTextAreaItem(nAreaID_1, pText, nTextColor, 0, nTextStyle,
                    pFontName, nFontHeight, nEffect, 30, 201, 3, pNULL, 0);
                if (nAreaItemID_1 == -1)
                {
                    Marshal.FreeHGlobal(pText);
                    Marshal.FreeHGlobal(pFontName);
                    nErrorCode = CSDKExport.Hd_GetSDKLastError();
                    return;
                }
                #endregion

                #region Show on Area 0_2: DÒNG TIÊU ĐỀ
                nFontHeight = 12;
                pText = Marshal.StringToHGlobalUni("VI TRI");
                nEffect = 0;

                int nAreaItemID_1_2 = CSDKExport.Hd_AddSimpleTextAreaItem(nAreaID_1_2, pText, nTextColor, 0, nTextStyle,
                    pFontName, nFontHeight, nEffect, 30, 201, 3, pNULL, 0);
                if (nAreaItemID_1_2 == -1)
                {
                    Marshal.FreeHGlobal(pText);
                    Marshal.FreeHGlobal(pFontName);
                    nErrorCode = CSDKExport.Hd_GetSDKLastError();
                    return;
                }
                #endregion

                #region Show on Area 1
                nFontHeight = 12;
                if (orderShows.Count > 0)
                {
                    //pText = Marshal.StringToHGlobalUni("37C00000" + "               " + orderShows[0].State1.ToUpper());
                    pText = Marshal.StringToHGlobalUni(orderShows[0].Vehicle.ToUpper());
                }
                else
                {
                    pText = Marshal.StringToHGlobalUni("");
                }
                nEffect = 0;

                int nAreaItemID_2 = CSDKExport.Hd_AddSimpleTextAreaItem(nAreaID_2, pText, nTextColor, 0, nTextStyle,
                    pFontName, nFontHeight, nEffect, 30, 201, 3, pNULL, 0);
                if (nAreaItemID_2 == -1)
                {
                    Marshal.FreeHGlobal(pText);
                    Marshal.FreeHGlobal(pFontName);
                    nErrorCode = CSDKExport.Hd_GetSDKLastError();
                    return;
                }
                #endregion

                #region Show on Area 1_2
                nFontHeight = 12;
                if (orderShows.Count > 0)
                {
                    //pText = Marshal.StringToHGlobalUni("37C00000" + "               " + orderShows[0].State1.ToUpper());
                    pText = Marshal.StringToHGlobalUni(orderShows[0].State1.ToUpper());
                }
                else
                {
                    pText = Marshal.StringToHGlobalUni("");
                }
                nEffect = 0;

                int nAreaItemID_2_2 = CSDKExport.Hd_AddSimpleTextAreaItem(nAreaID_2_2, pText, nTextColor, 0, nTextStyle,
                    pFontName, nFontHeight, nEffect, 30, 201, 3, pNULL, 0);
                if (nAreaItemID_2_2 == -1)
                {
                    Marshal.FreeHGlobal(pText);
                    Marshal.FreeHGlobal(pFontName);
                    nErrorCode = CSDKExport.Hd_GetSDKLastError();
                    return;
                }
                #endregion

                #region Show on Area 2
                nFontHeight = 12;
                if (orderShows.Count > 1)
                {
                    // pText = Marshal.StringToHGlobalUni("37C00000" + "               " + orderShows[1].State1.ToUpper());
                    pText = Marshal.StringToHGlobalUni(orderShows[1].Vehicle.ToUpper());
                }
                else
                {
                    pText = Marshal.StringToHGlobalUni("");
                }
                nEffect = 0;

                int nAreaItemID_3 = CSDKExport.Hd_AddSimpleTextAreaItem(nAreaID_3, pText, nTextColor, 0, nTextStyle,
                    pFontName, nFontHeight, nEffect, 30, 201, 3, pNULL, 0);
                if (nAreaItemID_3 == -1)
                {
                    Marshal.FreeHGlobal(pText);
                    Marshal.FreeHGlobal(pFontName);
                    nErrorCode = CSDKExport.Hd_GetSDKLastError();
                    return;
                }
                #endregion

                #region Show on Area 2_2
                nFontHeight = 12;
                if (orderShows.Count > 1)
                {
                    // pText = Marshal.StringToHGlobalUni("37C00000" + "               " + orderShows[1].State1.ToUpper());
                    pText = Marshal.StringToHGlobalUni(orderShows[1].State1.ToUpper());
                }
                else
                {
                    pText = Marshal.StringToHGlobalUni("");
                }
                nEffect = 0;

                int nAreaItemID_3_2 = CSDKExport.Hd_AddSimpleTextAreaItem(nAreaID_3_2, pText, nTextColor, 0, nTextStyle,
                    pFontName, nFontHeight, nEffect, 30, 201, 3, pNULL, 0);
                if (nAreaItemID_3_2 == -1)
                {
                    Marshal.FreeHGlobal(pText);
                    Marshal.FreeHGlobal(pFontName);
                    nErrorCode = CSDKExport.Hd_GetSDKLastError();
                    return;
                }
                #endregion

                #region Show on Area 3
                nFontHeight = 12;
                if (orderShows.Count > 2)
                {
                    //  pText = Marshal.StringToHGlobalUni("37C00000" + "               " + orderShows[2].State1.ToUpper());
                    pText = Marshal.StringToHGlobalUni(orderShows[2].Vehicle.ToUpper());
                }
                else
                {
                    pText = Marshal.StringToHGlobalUni("");
                }
                nEffect = 0;

                int nAreaItemID_4 = CSDKExport.Hd_AddSimpleTextAreaItem(nAreaID_4, pText, nTextColor, 0, nTextStyle,
                    pFontName, nFontHeight, nEffect, 30, 201, 3, pNULL, 0);
                if (nAreaItemID_4 == -1)
                {
                    Marshal.FreeHGlobal(pText);
                    Marshal.FreeHGlobal(pFontName);
                    nErrorCode = CSDKExport.Hd_GetSDKLastError();
                    return;
                }
                #endregion

                #region Show on Area 3_2
                nFontHeight = 12;
                if (orderShows.Count > 2)
                {
                    //  pText = Marshal.StringToHGlobalUni("37C00000" + "               " + orderShows[2].State1.ToUpper());
                    pText = Marshal.StringToHGlobalUni(orderShows[2].State1.ToUpper());
                }
                else
                {
                    pText = Marshal.StringToHGlobalUni("");
                }
                nEffect = 0;

                int nAreaItemID_4_2 = CSDKExport.Hd_AddSimpleTextAreaItem(nAreaID_4_2, pText, nTextColor, 0, nTextStyle,
                    pFontName, nFontHeight, nEffect, 30, 201, 3, pNULL, 0);
                if (nAreaItemID_4_2 == -1)
                {
                    Marshal.FreeHGlobal(pText);
                    Marshal.FreeHGlobal(pFontName);
                    nErrorCode = CSDKExport.Hd_GetSDKLastError();
                    return;
                }
                #endregion

                #region Show on Area 4
                nFontHeight = 12;
                if (orderShows.Count > 3)
                {
                    //pText = Marshal.StringToHGlobalUni("37C00000" + "               " + orderShows[3].State1.ToUpper());
                    pText = Marshal.StringToHGlobalUni(orderShows[3].Vehicle.ToUpper());
                }
                else
                {
                    pText = Marshal.StringToHGlobalUni("");
                }
                nEffect = 0;

                int nAreaItemID_5 = CSDKExport.Hd_AddSimpleTextAreaItem(nAreaID_5, pText, nTextColor, 0, nTextStyle,
                    pFontName, nFontHeight, nEffect, 30, 201, 3, pNULL, 0);
                if (nAreaItemID_5 == -1)
                {
                    Marshal.FreeHGlobal(pText);
                    Marshal.FreeHGlobal(pFontName);
                    nErrorCode = CSDKExport.Hd_GetSDKLastError();
                    return;
                }
                #endregion

                #region Show on Area 4_2
                nFontHeight = 12;
                if (orderShows.Count > 3)
                {
                    //pText = Marshal.StringToHGlobalUni("37C00000" + "               " + orderShows[3].State1.ToUpper());
                    pText = Marshal.StringToHGlobalUni(orderShows[3].State1.ToUpper());
                }
                else
                {
                    pText = Marshal.StringToHGlobalUni("");
                }
                nEffect = 0;

                int nAreaItemID_5_2 = CSDKExport.Hd_AddSimpleTextAreaItem(nAreaID_5_2, pText, nTextColor, 0, nTextStyle,
                    pFontName, nFontHeight, nEffect, 30, 201, 3, pNULL, 0);
                if (nAreaItemID_5_2 == -1)
                {
                    Marshal.FreeHGlobal(pText);
                    Marshal.FreeHGlobal(pFontName);
                    nErrorCode = CSDKExport.Hd_GetSDKLastError();
                    return;
                }
                #endregion

                #region Show on Area 5
                nFontHeight = 12;
                if (orderShows.Count > 4)
                {
                    // pText = Marshal.StringToHGlobalUni("37C00000" + "               " + orderShows[4].State1.ToUpper());
                    pText = Marshal.StringToHGlobalUni(orderShows[4].Vehicle.ToUpper());
                }
                else
                {
                    pText = Marshal.StringToHGlobalUni("");
                }
                nEffect = 0;

                int nAreaItemID_6 = CSDKExport.Hd_AddSimpleTextAreaItem(nAreaID_6, pText, nTextColor, 0, nTextStyle,
                    pFontName, nFontHeight, nEffect, 30, 201, 3, pNULL, 0);
                if (nAreaItemID_6 == -1)
                {
                    Marshal.FreeHGlobal(pText);
                    Marshal.FreeHGlobal(pFontName);
                    nErrorCode = CSDKExport.Hd_GetSDKLastError();
                    return;
                }
                #endregion

                #region Show on Area 5_2
                nFontHeight = 12;
                if (orderShows.Count > 4)
                {
                    // pText = Marshal.StringToHGlobalUni("37C00000" + "               " + orderShows[4].State1.ToUpper());
                    pText = Marshal.StringToHGlobalUni(orderShows[4].State1.ToUpper());
                }
                else
                {
                    pText = Marshal.StringToHGlobalUni("");
                }
                nEffect = 0;

                int nAreaItemID_6_2 = CSDKExport.Hd_AddSimpleTextAreaItem(nAreaID_6_2, pText, nTextColor, 0, nTextStyle,
                    pFontName, nFontHeight, nEffect, 30, 201, 3, pNULL, 0);
                if (nAreaItemID_6_2 == -1)
                {
                    Marshal.FreeHGlobal(pText);
                    Marshal.FreeHGlobal(pFontName);
                    nErrorCode = CSDKExport.Hd_GetSDKLastError();
                    return;
                }
                #endregion

                Marshal.FreeHGlobal(pText);
                Marshal.FreeHGlobal(pFontName);

                // 5. Send to device 
                nRe = CSDKExport.Hd_SendScreen(m_nSendType_LED12, m_pSendParams_LED12, pNULL, pNULL, 0);
                if (nRe != 0)
                {
                    nErrorCode = CSDKExport.Hd_GetSDKLastError();
                }

                log.Info($"end show led xi roi - nErrorCode: {nErrorCode}");
                Console.WriteLine($"end show led roi - nErrorCode: {nErrorCode}");
            }
            catch (Exception ex)
            {
                log.Info($"ShowLed12Process LedXiRoi error: {ex.StackTrace} {ex.Message}");
            }
        }
        private void SetLED12NoContent()
        {
            try
            {
                IntPtr pNULL = new IntPtr(0);

                int nErrorCode = -1;
                // 1. Create a screen
                int nWidth = 160;
                int nHeight = 112;
                int nColor = 1;
                int nGray = 1;
                int nCardType = 0;

                int nRe = CSDKExport.Hd_CreateScreen(nWidth, nHeight, nColor, nGray, nCardType, pNULL, 0);
                if (nRe != 0)
                {
                    nErrorCode = CSDKExport.Hd_GetSDKLastError();
                    return;
                }

                // 2. Add program to screen
                int nProgramID = CSDKExport.Hd_AddProgram(pNULL, 0, 0, pNULL, 0);
                if (nProgramID == -1)
                {
                    nErrorCode = CSDKExport.Hd_GetSDKLastError();
                    return;
                }

                #region Add Area 0
                int nX1 = 0;
                int nY1 = 20;
                int nAreaWidth = 160;
                int nAreaHeight = 20;

                int nAreaID_1 = CSDKExport.Hd_AddArea(nProgramID, nX1, nY1, nAreaWidth, nAreaHeight, pNULL, 0, 0, pNULL, 0);
                if (nAreaID_1 == -1)
                {
                    nErrorCode = CSDKExport.Hd_GetSDKLastError();
                    return;
                }
                #endregion

                #region Add Area 1
                int nX2 = 0;
                int nY2 = 50;

                int nAreaID_2 = CSDKExport.Hd_AddArea(nProgramID, nX2, nY2, nAreaWidth, nAreaHeight, pNULL, 0, 0, pNULL, 0);
                if (nAreaID_2 == -1)
                {
                    nErrorCode = CSDKExport.Hd_GetSDKLastError();
                    return;
                }
                #endregion

                #region Add Area 2
                int nX3 = 0;
                int nY3 = 70;

                int nAreaID_3 = CSDKExport.Hd_AddArea(nProgramID, nX3, nY3, nAreaWidth, nAreaHeight, pNULL, 0, 0, pNULL, 0);
                if (nAreaID_3 == -1)
                {
                    nErrorCode = CSDKExport.Hd_GetSDKLastError();
                    return;
                }
                #endregion

                #region DÒNG TIÊU ĐỀ
                // 4.Add text AreaItem to Area
                IntPtr pText = Marshal.StringToHGlobalUni("-VICEM HẢI PHÒNG-");
                IntPtr pFontName = Marshal.StringToHGlobalUni("Times New Roman");
                int nTextColor = CSDKExport.Hd_GetColor(255, 255, 255);

                // center in bold and underline
                int nTextStyle = 0x0004 | 0x0100; /*| 0x0200 */
                int nFontHeight = 14;
                int nEffect = 0;
                #endregion

                #region Show on Area 0
                int nAreaItemID_1 = CSDKExport.Hd_AddSimpleTextAreaItem(nAreaID_1, pText, nTextColor, 0, nTextStyle,
                    pFontName, nFontHeight, nEffect, 30, 201, 3, pNULL, 0);
                if (nAreaItemID_1 == -1)
                {
                    Marshal.FreeHGlobal(pText);
                    Marshal.FreeHGlobal(pFontName);
                    nErrorCode = CSDKExport.Hd_GetSDKLastError();
                    return;
                }
                #endregion

                #region Show on Area 1
                nFontHeight = 12;
                pText = Marshal.StringToHGlobalUni("HỆ THỐNG");
                nEffect = 0;
                int nAreaItemID_2 = CSDKExport.Hd_AddSimpleTextAreaItem(nAreaID_2, pText, nTextColor, 0, nTextStyle,
                    pFontName, nFontHeight, nEffect, 30, 201, 3, pNULL, 0);
                if (nAreaItemID_2 == -1)
                {
                    Marshal.FreeHGlobal(pText);
                    Marshal.FreeHGlobal(pFontName);
                    nErrorCode = CSDKExport.Hd_GetSDKLastError();
                    return;
                }
                #endregion

                #region Show on Area 2
                nFontHeight = 12;
                pText = Marshal.StringToHGlobalUni("XUẤT HÀNG TỰ ĐỘNG");
                nEffect = 0;
                int nAreaItemID_3 = CSDKExport.Hd_AddSimpleTextAreaItem(nAreaID_3, pText, nTextColor, 0, nTextStyle,
                    pFontName, nFontHeight, nEffect, 30, 201, 3, pNULL, 0);
                if (nAreaItemID_3 == -1)
                {
                    Marshal.FreeHGlobal(pText);
                    Marshal.FreeHGlobal(pFontName);
                    nErrorCode = CSDKExport.Hd_GetSDKLastError();
                    return;
                }
                #endregion

                Marshal.FreeHGlobal(pText);
                Marshal.FreeHGlobal(pFontName);

                // 5. Send to device 
                nRe = CSDKExport.Hd_SendScreen(m_nSendType_LED12, m_pSendParams_LED12, pNULL, pNULL, 0);
                if (nRe != 0)
                {
                    nErrorCode = CSDKExport.Hd_GetSDKLastError();
                }

                log.Info($"end show led xi roi: no content - nErrorCode: {nErrorCode}");
                Console.WriteLine($"end show led roi: no content - nErrorCode: {nErrorCode}");
            }
            catch (Exception ex)
            {
                log.Info($"SetLED12NoContent LedXiRoi error: {ex.StackTrace} {ex.Message}");
            }
        }
    }
}
