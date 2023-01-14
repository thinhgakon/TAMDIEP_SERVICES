﻿using Quartz;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using XHTD_SERVICES_LED.Libs;
using XHTD_SERVICES_LED.Models.Response;

namespace XHTD_SERVICES_LED.Jobs
{
    public class LedXiBao : IJob
    {
        int m_nSendType_LED12;
        IntPtr m_pSendParams_LED12;
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger
      (System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        public LedXiBao()
        {
            m_nSendType_LED12 = 0;
            string strParams_LED12 = "10.0.7.1";
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
        public void ShowLed12Process()
        {
            // đổ data thay thế vào đây

            //nếu không có data thì sử dụng màn hình led với thông tin mong muốn ở hàm  SetLED12NoContent

            log.Info("start show led bao");


            var orderShows = new List<StoreOrderForLED12>();
            orderShows.Add(new StoreOrderForLED12 { Vehicle = "37H01011", State1 = "dang moi vao" }); //1
            orderShows.Add(new StoreOrderForLED12 { Vehicle = "37H01012", State1 = "dang moi vao" }); //2
            orderShows.Add(new StoreOrderForLED12 { Vehicle = "37H01013", State1 = "dang moi vao" }); //3
            orderShows.Add(new StoreOrderForLED12 { Vehicle = "37H01014", State1 = "dang moi vao" }); //4
            orderShows.Add(new StoreOrderForLED12 { Vehicle = "37H01015", State1 = "dang moi vao" }); //5
            orderShows.Add(new StoreOrderForLED12 { Vehicle = "37H01016", State1 = "dang moi vao" }); //6
            orderShows.Add(new StoreOrderForLED12 { Vehicle = "37H01017", State1 = "dang moi vao" }); //7
            orderShows.Add(new StoreOrderForLED12 { Vehicle = "37H01018", State1 = "dang moi vao" }); //8
            orderShows.Add(new StoreOrderForLED12 { Vehicle = "37H01019", State1 = "dang moi vao" }); //9


            IntPtr pNULL = new IntPtr(0);

            int nErrorCode = -1;
            // 1. Create a screen
            int nWidth = 224;
            int nHeight = 160;
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

            int plusX = 12;
            int plusY = 6;
            #region Add Area 0
            int nX1 = 12;
            int nY1 = 6;
            int nAreaWidth = 224 - plusX;
            int nAreaHeight = 16;

            int nAreaID_1 = CSDKExport.Hd_AddArea(nProgramID, nX1, nY1, nAreaWidth, nAreaHeight, pNULL, 0, 0, pNULL, 0);
            if (nAreaID_1 == -1)
            {
                nErrorCode = CSDKExport.Hd_GetSDKLastError();
                return;
            }
            #endregion

            #region Add Area 1
            int nX2 = 0 + plusX;
            int nY2 = 16 + plusY;


            int nAreaID_2 = CSDKExport.Hd_AddArea(nProgramID, nX2, nY2, nAreaWidth, nAreaHeight, pNULL, 0, 0, pNULL, 0);
            if (nAreaID_2 == -1)
            {
                nErrorCode = CSDKExport.Hd_GetSDKLastError();
                return;
            }
            #endregion

            #region Add Area 2
            int nX3 = 0 + plusX;
            int nY3 = 32 + plusY;

            int nAreaID_3 = CSDKExport.Hd_AddArea(nProgramID, nX3, nY3, nAreaWidth, nAreaHeight, pNULL, 0, 0, pNULL, 0);
            if (nAreaID_3 == -1)
            {
                nErrorCode = CSDKExport.Hd_GetSDKLastError();
                return;
            }
            #endregion

            #region Add Area 3
            int nX4 = 0 + plusX;
            int nY4 = 48 + plusY;

            int nAreaID_4 = CSDKExport.Hd_AddArea(nProgramID, nX4, nY4, nAreaWidth, nAreaHeight, pNULL, 0, 0, pNULL, 0);
            if (nAreaID_4 == -1)
            {
                nErrorCode = CSDKExport.Hd_GetSDKLastError();
                return;
            }
            #endregion

            #region Add Area 4
            int nX5 = 0 + plusX;
            int nY5 = 64 + plusY;

            int nAreaID_5 = CSDKExport.Hd_AddArea(nProgramID, nX5, nY5, nAreaWidth, nAreaHeight, pNULL, 0, 0, pNULL, 0);
            if (nAreaID_5 == -1)
            {
                nErrorCode = CSDKExport.Hd_GetSDKLastError();
                return;
            }
            #endregion

            #region Add Area 5
            int nX6 = 0 + plusX;
            int nY6 = 80 + plusY;

            int nAreaID_6 = CSDKExport.Hd_AddArea(nProgramID, nX6, nY6, nAreaWidth, nAreaHeight, pNULL, 0, 0, pNULL, 0);
            if (nAreaID_6 == -1)
            {
                nErrorCode = CSDKExport.Hd_GetSDKLastError();
                return;
            }
            #endregion

            #region Add Area 6
            int nX7 = 0 + plusX;
            int nY7 = 96 + plusY;

            int nAreaID_7 = CSDKExport.Hd_AddArea(nProgramID, nX7, nY7, nAreaWidth, nAreaHeight, pNULL, 0, 0, pNULL, 0);
            if (nAreaID_7 == -1)
            {
                nErrorCode = CSDKExport.Hd_GetSDKLastError();
                return;
            }
            #endregion

            #region Add Area 7
            int nX8 = 0 + plusX;
            int nY8 = 112 + plusY;

            int nAreaID_8 = CSDKExport.Hd_AddArea(nProgramID, nX8, nY8, nAreaWidth, nAreaHeight, pNULL, 0, 0, pNULL, 0);
            if (nAreaID_8 == -1)
            {
                nErrorCode = CSDKExport.Hd_GetSDKLastError();
                return;
            }
            #endregion
            #region Add Area 8
            int nX9 = 0 + plusX;
            int nY9 = 128 + plusY;

            int nAreaID_9 = CSDKExport.Hd_AddArea(nProgramID, nX9, nY9, nAreaWidth, nAreaHeight, pNULL, 0, 0, pNULL, 0);
            if (nAreaID_9 == -1)
            {
                nErrorCode = CSDKExport.Hd_GetSDKLastError();
                return;
            }
            #endregion
            #region Add Area 9
            //int nX10 = 0 + plusX;
            //int nY10 = 144 + plusY;

            //int nAreaID_10 = CSDKExport.Hd_AddArea(nProgramID, nX10, nY10, nAreaWidth, nAreaHeight, pNULL, 0, 0, pNULL, 0);
            //if (nAreaID_10 == -1)
            //{
            //    nErrorCode = CSDKExport.Hd_GetSDKLastError();
            //    return;
            //}
            #endregion

            #region DÒNG TIÊU ĐỀ
            // 4.Add text AreaItem to Area
            IntPtr pText = Marshal.StringToHGlobalUni("BIEN SO." + "       " + "TRANG THAI");
            IntPtr pFontName = Marshal.StringToHGlobalUni("Times New Roman");
            //int nTextColor = CSDKExport.Hd_GetColor(255, 0, 0);
            int nTextColor = CSDKExport.Hd_GetColor(255, 255, 255);
            int nTextStyle = 0x0000 | 0x0100 /*| 0x0200 */;
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
            nFontHeight = 14;
            if (orderShows.Count > 0)
            {
                //pText = Marshal.StringToHGlobalUni("37C00000" + "       " + orderShows[0].State1.ToUpper());
                pText = Marshal.StringToHGlobalUni(orderShows[0].Vehicle.ToUpper() + "       " + (orderShows[0].State1.ToUpper()));
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

            #region Show on Area 2
            nFontHeight = 14;
            if (orderShows.Count > 1)
            {
                // pText = Marshal.StringToHGlobalUni("37C00000" + "       " + orderShows[1].State1.ToUpper());
                pText = Marshal.StringToHGlobalUni(orderShows[1].Vehicle.ToUpper() + "       " + (orderShows[1].State1.ToUpper()));
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

            #region Show on Area 3
            nFontHeight = 14;
            if (orderShows.Count > 2)
            {
                //  pText = Marshal.StringToHGlobalUni("37C00000" + "       " + orderShows[2].State1.ToUpper());
                pText = Marshal.StringToHGlobalUni(orderShows[2].Vehicle.ToUpper() + "       " + (orderShows[2].State1.ToUpper()));
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

            #region Show on Area 4
            nFontHeight = 14;
            if (orderShows.Count > 3)
            {
                //pText = Marshal.StringToHGlobalUni("37C00000" + "       " + orderShows[3].State1.ToUpper());
                pText = Marshal.StringToHGlobalUni(orderShows[3].Vehicle.ToUpper() + "       " + (orderShows[3].State1.ToUpper()));
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

            #region Show on Area 5
            nFontHeight = 14;
            if (orderShows.Count > 4)
            {
                // pText = Marshal.StringToHGlobalUni("37C00000" + "       " + orderShows[4].State1.ToUpper());
                pText = Marshal.StringToHGlobalUni(orderShows[4].Vehicle.ToUpper() + "       " + (orderShows[4].State1.ToUpper()));
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

            #region Show on Area 6
            nFontHeight = 14;
            if (orderShows.Count > 5)
            {
                //pText = Marshal.StringToHGlobalUni("37C00000" + "       " + orderShows[5].State1.ToUpper());
                pText = Marshal.StringToHGlobalUni(orderShows[5].Vehicle.ToUpper() + "       " + (orderShows[5].State1.ToUpper()));
            }
            else
            {
                pText = Marshal.StringToHGlobalUni("");
            }
            nEffect = 0;
            int nAreaItemID_7 = CSDKExport.Hd_AddSimpleTextAreaItem(nAreaID_7, pText, nTextColor, 0, nTextStyle,
                pFontName, nFontHeight, nEffect, 30, 201, 3, pNULL, 0);
            if (nAreaItemID_7 == -1)
            {
                Marshal.FreeHGlobal(pText);
                Marshal.FreeHGlobal(pFontName);
                nErrorCode = CSDKExport.Hd_GetSDKLastError();
                return;
            }
            #endregion
            // mới thêm
            #region Show on Area 7
            nFontHeight = 14;
            if (orderShows.Count > 6)
            {
                // pText = Marshal.StringToHGlobalUni("37C00000" + "       " + orderShows[6].State1.ToUpper());
                pText = Marshal.StringToHGlobalUni(orderShows[6].Vehicle.ToUpper() + "       " + (orderShows[6].State1.ToUpper()));
            }
            else
            {
                pText = Marshal.StringToHGlobalUni("");
            }
            nEffect = 0;
            int nAreaItemID_8 = CSDKExport.Hd_AddSimpleTextAreaItem(nAreaID_8, pText, nTextColor, 0, nTextStyle,
                pFontName, nFontHeight, nEffect, 30, 201, 3, pNULL, 0);
            if (nAreaItemID_8 == -1)
            {
                Marshal.FreeHGlobal(pText);
                Marshal.FreeHGlobal(pFontName);
                nErrorCode = CSDKExport.Hd_GetSDKLastError();
                return;
            }
            #endregion

            #region Show on Area 8
            nFontHeight = 14;
            if (orderShows.Count > 7)
            {
                //pText = Marshal.StringToHGlobalUni("37C00000" + "       " + orderShows[7].State1.ToUpper());
                pText = Marshal.StringToHGlobalUni(orderShows[7].Vehicle.ToUpper() + "       " + (orderShows[7].State1.ToUpper()));
            }
            else
            {
                pText = Marshal.StringToHGlobalUni("");
            }
            nEffect = 0;
            int nAreaItemID_9 = CSDKExport.Hd_AddSimpleTextAreaItem(nAreaID_9, pText, nTextColor, 0, nTextStyle,
                pFontName, nFontHeight, nEffect, 30, 201, 3, pNULL, 0);
            if (nAreaItemID_9 == -1)
            {
                Marshal.FreeHGlobal(pText);
                Marshal.FreeHGlobal(pFontName);
                nErrorCode = CSDKExport.Hd_GetSDKLastError();
                return;
            }
            #endregion

            #region Show on Area 9
            //nFontHeight = 14;
            //if (orderShows.Count > 8)
            //{
            //    pText = Marshal.StringToHGlobalUni("37C00000" + "       " + orderShows[8].State1.ToUpper());
            //}
            //else
            //{
            //    pText = Marshal.StringToHGlobalUni("");
            //}
            //nEffect = 0;
            //int nAreaItemID_10 = CSDKExport.Hd_AddSimpleTextAreaItem(nAreaID_10, pText, nTextColor, 0, nTextStyle,
            //    pFontName, nFontHeight, nEffect, 30, 201, 3, pNULL, 0);
            //if (nAreaItemID_10 == -1)
            //{
            //    Marshal.FreeHGlobal(pText);
            //    Marshal.FreeHGlobal(pFontName);
            //    nErrorCode = CSDKExport.Hd_GetSDKLastError();
            //    return;
            //}
            #endregion



            Marshal.FreeHGlobal(pText);
            Marshal.FreeHGlobal(pFontName);

            // 5. Send to device 
            nRe = CSDKExport.Hd_SendScreen(m_nSendType_LED12, m_pSendParams_LED12, pNULL, pNULL, 0);
            if (nRe != 0)
            {
                nErrorCode = CSDKExport.Hd_GetSDKLastError();
            }

            log.Info("end show led xi bao");
        }
        private void SetLED12NoContent()
        {
            try
            {
                IntPtr pNULL = new IntPtr(0);

                int nErrorCode = -1;
                // 1. Create a screen
                int nWidth = 224;
                int nHeight = 160;
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
                int nY1 = 40;
                int nAreaWidth = 224;
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
                int nY2 = 70;


                int nAreaID_2 = CSDKExport.Hd_AddArea(nProgramID, nX2, nY2, nAreaWidth, nAreaHeight, pNULL, 0, 0, pNULL, 0);
                if (nAreaID_2 == -1)
                {
                    nErrorCode = CSDKExport.Hd_GetSDKLastError();
                    return;
                }
                #endregion

                #region Add Area 2
                int nX3 = 0;
                int nY3 = 90;

                int nAreaID_3 = CSDKExport.Hd_AddArea(nProgramID, nX3, nY3, nAreaWidth, nAreaHeight, pNULL, 0, 0, pNULL, 0);
                if (nAreaID_3 == -1)
                {
                    nErrorCode = CSDKExport.Hd_GetSDKLastError();
                    return;
                }
                #endregion



                #region DÒNG TIÊU ĐỀ
                // 4.Add text AreaItem to Area
                IntPtr pText = Marshal.StringToHGlobalUni("-VICEM HOÀNG MAI-");
                IntPtr pFontName = Marshal.StringToHGlobalUni("Times New Roman");
                int nTextColor = CSDKExport.Hd_GetColor(255, 255, 255);

                // center in bold and underline
                int nTextStyle = 0x0004 | 0x0100; /*| 0x0200 */
                int nFontHeight = 18;
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
                nFontHeight = 14;
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
                nFontHeight = 14;
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
            }
            catch (Exception ex)
            {
            }
        }
    }
}
