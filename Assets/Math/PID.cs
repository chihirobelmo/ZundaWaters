using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static StaticMath;

/// <summary>
/// from https://controlabo.com/pid-program/
/// </summary>
public class PID
{
    // 定数設定 ===============
    float KP = 0.001f;  // Pゲイン
    float KI = 0;   // Iゲイン
    float KD = 0;   // Dゲイン

    // 変数初期化 ===============
    long e_pre = 0; // 微分の近似計算のための初期値
    long ie = 0;    // 積分の近似計算のための初期値

    public void reset()
    {
        e_pre = 0;
        ie = 0;
    }

    public PID() { }

    /// <summary>
    /// KP,KI,KD
    /// </summary>
    public PID(float kp, float ki, float kd) {
        KP = kp;
        KI = ki;
        KD = kd;
    }
    public void SetPID(float kp, float ki, float kd)
    {
        KP = kp;
        KI = ki;
        KD = kd;
    }

    const int significant = 1000;

    public float run(float current, float target, float dt)
    {
        // 現時刻における情報を取得
        long y = (long)(current * 1000); // 出力を取得。例:センサー情報を読み取る処理
        long r = (long)(target * 1000); // 目標値を取得。目標値が一定ならその値を代入する
        long t = (long)(dt * 1000);

        // PID制御の式より、制御入力uを計算
        long e = r - y;                // 誤差を計算
        ie += (e + e_pre) * (/*ESP*/1 + t) / 2;     // 誤差の積分を近似計算
        long de = (e - e_pre) / (/*ESP*/1 + t);     // 誤差の微分を近似計算
        float u = KP * e + KI * ie + KD * de; // PID制御の式にそれぞれを代入

        // 次のために現時刻の情報を記録
        e_pre = e;

        return u / 1000;
    }
}

/* https://qiita.com/RyoH_/items/373f6451c4946b1e447e
パラメータチューニング
PID制御では
,
,
の値を設定する必要があります．様々な方法がありますが，試行錯誤で適切な値を探ることにします．

ステップ１
,
KI,KDの値を0とし，
KPのみで制御する．
KPの値が小さければ，自動車の振動は小さいものの，目標に達するまで時間がかかります（本シミュレーションの場合はハンドルを切るのが遅くてコースアウトしてしまいます）．逆に
KPの値を大きくしすぎると，自動車が目標値を超えて大きく振動してしまいます．小さすぎず，大きすぎない値を見つける必要があります．

ステップ２
KDに値を入れてみる．次に微分ゲインに数値を入れてみます．
KDが小さいとほぼ先ほどと大きくは変わりません．大きくしすぎると，ハンドル角の変化が小さくなりすぎてしまい，制御することできなくなります．適切な値を選ぶと，オーバーシュートが小さくなり，ほぼ目標値に追従して進むようになります．
*/