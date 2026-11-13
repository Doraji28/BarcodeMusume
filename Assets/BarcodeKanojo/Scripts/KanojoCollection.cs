using System.Collections.Generic;
using UnityEngine;

namespace BarcodeKanojo
{
    /// <summary>
    /// 만난 카노죠 목록(도감). 캐릭터는 바코드만 있으면 언제든 똑같이 다시 생성되므로 바코드 문자열만 저장한다.
    /// </summary>
    public static class KanojoCollection
    {
        const string Key = "kanojo.collection";

        public static List<string> All()
        {
            string raw = PlayerPrefs.GetString(Key, "");
            var list = new List<string>();
            foreach (var s in raw.Split(';'))
                if (!string.IsNullOrEmpty(s)) list.Add(s);
            return list;
        }

        public static int Count => All().Count;

        /// <summary>추가하고, 처음 만난 바코드였으면 true. 최근 것이 앞으로 온다.</summary>
        public static bool Add(string barcode)
        {
            var list = All();
            bool isNew = !list.Remove(barcode);
            list.Insert(0, barcode);
            PlayerPrefs.SetString(Key, string.Join(";", list));
            PlayerPrefs.Save();
            return isNew;
        }
    }
}
