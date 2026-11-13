using System;

namespace BarcodeKanojo
{
    /// <summary>
    /// 바코드 하나에서 결정론적으로 생성된 카노죠 한 명의 파츠/성격 값.
    /// 값의 범위는 팬 복원 서버(derand/kanojo_server)의 generate()를 따른다.
    /// </summary>
    [Serializable]
    public class KanojoData
    {
        public string barcode;

        // 색상
        public int hairColor;     // 1~24
        public int eyeColor;      // 1~12
        public int skinColor;     // 1~12
        public int clothesColor;  // 1~6

        // 파츠
        public int mouthType;     // 1~12
        public int raceType;      // 1~10
        public int spotType;      // 1~7
        public int clothesType;   // 1~5
        public int browType;      // 1~12
        public int accessoryType; // 1~5
        public int hairType;      // 1~26
        public int earType;       // 1~2
        public int glassesType;   // 1~2
        public int faceType;      // 1~6
        public int eyeType;       // 101~115
        public int fringeType;    // 1~22 (앞머리)
        public int noseType;      // 1~6
        public int bodyType = 1;  // 고정

        // 위치 보정
        public int eyePosition;   // -10~10
        public int browPosition;  // -10~10
        public int mouthPosition; // -10~10

        // 성격
        public int consumption;   // 1~100
        public int possession;    // 1~100
        public int recognition;   // 1~100
        public int sexual;        // 1~100
        public int flirtable;     // 1~100

        public override string ToString()
        {
            return $"hair_color {hairColor}, eye_color {eyeColor}, skin_color {skinColor}, clothes_color {clothesColor}, " +
                   $"mouth {mouthType}, race {raceType}, spot {spotType}, clothes {clothesType}, brow {browType}, " +
                   $"accessory {accessoryType}, hair {hairType}, ear {earType}, glasses {glassesType}, face {faceType}, " +
                   $"eye {eyeType}, fringe {fringeType}, nose {noseType}, eye_pos {eyePosition}, brow_pos {browPosition}, " +
                   $"mouth_pos {mouthPosition}, consumption {consumption}, possession {possession}, " +
                   $"recognition {recognition}, sexual {sexual}, flirtable {flirtable}";
        }
    }
}
