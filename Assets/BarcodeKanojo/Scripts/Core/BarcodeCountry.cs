namespace BarcodeKanojo
{
    /// <summary>EAN-13 앞자리(GS1 접두어)로 "출신 지역"을 대략 알려준다.</summary>
    public static class BarcodeCountry
    {
        public static string Of(string barcode)
        {
            if (string.IsNullOrEmpty(barcode) || barcode.Length != 13) return "알 수 없음";
            if (!int.TryParse(barcode.Substring(0, 3), out int p)) return "알 수 없음";

            if (p <= 139) return "미국·캐나다";
            if (p >= 300 && p <= 379) return "프랑스";
            if (p >= 400 && p <= 440) return "독일";
            if ((p >= 450 && p <= 459) || (p >= 490 && p <= 499)) return "일본";
            if (p >= 460 && p <= 469) return "러시아";
            if (p == 471) return "대만";
            if (p == 489) return "홍콩";
            if (p >= 500 && p <= 509) return "영국";
            if (p >= 690 && p <= 699) return "중국";
            if (p >= 760 && p <= 769) return "스위스";
            if (p >= 800 && p <= 839) return "이탈리아";
            if (p >= 840 && p <= 849) return "스페인";
            if (p == 880) return "대한민국";
            if (p == 885) return "태국";
            if (p == 888) return "싱가포르";
            if (p == 893) return "베트남";
            if (p >= 930 && p <= 939) return "호주";
            if (p >= 940 && p <= 949) return "뉴질랜드";
            if (p == 977) return "정기간행물";
            if (p == 978 || p == 979) return "도서";
            if (p >= 200 && p <= 299) return "점포 내부 코드";
            return "기타 지역";
        }
    }
}
