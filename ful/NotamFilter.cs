using System;
using System.Collections.Generic;
using System.Text;

namespace FUL
{
    public enum NotamPurpose { All, ExcludingMiscellaneous, Miscellaneous };

    public class NotamFilter
    {
        private bool domestic;

        private bool fdc;

        private bool military;
        private int militaryValidityMinHour;
        private int militaryValidityMaxHour;
        private string militaryNotamCodes;

        private bool international;
        private string internationalNotamCodes;
        private int internationalMinLevel;
        private int internationalMaxLevel;
        private NotamPurpose internationalPurpose;
        private int internationalValidityMinHour;
        private int internationalValidityMaxHour;

        public NotamFilter()
        {
            domestic = true;
            fdc = true;
            military = true;
            international = true;

            militaryNotamCodes = "All";
            militaryValidityMinHour = 1;
            militaryValidityMaxHour = 6;

            internationalNotamCodes = "All";
            internationalMinLevel = 0;
            internationalMaxLevel = 999;
            internationalPurpose = NotamPurpose.All;
            internationalValidityMinHour = 1;
            internationalValidityMaxHour = 6;
        }

        public bool Domestic
        {
            get { return domestic; }
            set { domestic = value; }
        }

        public bool FDC
        {
            get { return fdc; }
            set { fdc = value; }
        }

        public bool Military
        {
            get { return military; }
            set { military = value; }
        }

        public bool International
        {
            get { return international; }
            set { international = value; }
        }

        public string MilitaryNotamCodes
        {
            get { return militaryNotamCodes; }
            set { militaryNotamCodes = value; }
        }

        public int MilitaryValidityMinHour
        {
            get { return militaryValidityMinHour; }
            set { militaryValidityMinHour = value; }
        }

        public int MilitaryValidityMaxHour
        {
            get { return militaryValidityMaxHour; }
            set { militaryValidityMaxHour = value; }
        }

        public string InternationalNotamCodes
        {
            get { return internationalNotamCodes; }
            set { internationalNotamCodes = value; }
        }

        public int InternationalMinLevel
        {
            get { return internationalMinLevel; }
            set { internationalMinLevel = value; }
        }

        public int InternationalMaxLevel
        {
            get { return internationalMaxLevel; }
            set { internationalMaxLevel = value; }
        }

        public NotamPurpose InternationalPurpose
        {
            get { return internationalPurpose; }
            set { internationalPurpose = value; }
        }

        public int InternationalValidityMinHour
        {
            get { return internationalValidityMinHour; }
            set { internationalValidityMinHour = value; }
        }

        public int InternationalValidityMaxHour
        {
            get { return internationalValidityMaxHour; }
            set { internationalValidityMaxHour = value; }
        }
    }
}
