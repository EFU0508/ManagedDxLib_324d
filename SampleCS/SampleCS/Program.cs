﻿using System;
using System.Collections.Generic;
using System.Threading;
using static DX;

namespace Sample
{
    internal static class Program
    {
        struct effect
        {
            public float Alpha;
            public float Size;
            public VECTOR Position;
        }

        /// <summary>
        /// アプリケーションのメイン エントリ ポイントです。
        /// </summary>
        [STAThread]
        static void Main()
        {
            SetOutApplicationLogValidFlag(FALSE);               /*log*/
            SetMainWindowText("ManagedDxLib for C#");              /*タイトル*/
            ChangeWindowMode(TRUE);                     /*窓表示*/
            SetUseDirect3DVersion(DX_DIRECT3D_11);              /*directX ver*/
            SetGraphMode(1280, 720, 16);

            int ret = DxLib_Init();
            if (ret < 0)
            {
                throw new Exception("DxLib_Init Error");
            }

            SetUseDirectInputFlag(TRUE);                        /*DirectInput使用*/
            SetDirectInputMouseMode(FALSE);                     /*DirectInputマウス使用*/
            SetWindowSizeChangeEnableFlag(FALSE, TRUE);         /*ウインドウサイズを手動変更不可、ウインドウサイズに合わせて拡大*/
            SetUsePixelLighting(TRUE);                          /*ピクセルライティングの使用*/
            SetFullSceneAntiAliasingMode(4, 2);                 /*アンチエイリアス*/
            SetEnableXAudioFlag(TRUE);                          /*XAudioを用いるか*/
            Set3DSoundOneMetre(1.0f);                           /*3Dオーディオの基準距離指定*/
            SetWaitVSyncFlag(FALSE);                             /*垂直同期*/
            SetAlwaysRunFlag(TRUE);                             /* 非アクティブでも動作*/
            SetUseDXArchiveFlag(TRUE);                          /* dxaファイルをフォルダとする */
            SetWindowUserCloseEnableFlag(FALSE);                /* ×で勝手Windowを閉じないようにする*/
            SetDrawScreen(DX_SCREEN_BACK);                      /* 描画先を裏画面にセット */
            MV1SetLoadModelUsePhysicsMode(DX_LOADMODEL_PHYSICS_LOADCALC);
            MV1SetLoadModelPhysicsWorldGravity(-9.8f);
            SetCameraNearFar(0.1f, 1000.0f);                    /* 奥行0.1～1000までをカメラの描画範囲とする */
            SetUseLighting(TRUE);                                /* ライティングを考慮しないモード */
            SetMouseDispFlag(FALSE);                  /*マウス表示*/

            // フルスクリーンウインドウの切り替えでリソースが消えるのを防ぐ。
            SetChangeScreenModeGraphicsSystemResetFlag(FALSE);

            //Zバッファを有効にする
            SetUseZBuffer3D(TRUE);

            //Zバッファの書き込みを有効にする
            SetWriteZBuffer3D(TRUE);

            VECTOR pos = VGet(0, 0, 0);

            VECTOR direction = VGet(0.0f, 1.0f, 1.0f);
            float brightness = 0.6f;

            // 平行光源の作成
            int lightHandle = CreateDirLightHandle(direction);

            // 平行光源の方向を設定
            SetLightDirectionHandle(lightHandle, direction);

            // ディフューズカラーの設定（白色光）
            SetLightDifColorHandle(lightHandle, GetColorF(brightness, brightness, brightness, 1f));

            // スペキュラカラーの設定（白色光）
            SetLightSpcColorHandle(lightHandle, GetColorF(brightness, brightness, brightness, 1f));

            // アンビエントカラーの設定（弱い白色光）
            SetLightAmbColorHandle(lightHandle, GetColorF(brightness, brightness, brightness, 1f));

            // ライトを有効にする
            SetLightEnableHandle(lightHandle, TRUE);

            // カメラ位置更新
            SetCameraPositionAndTarget_UpVecY(VGet(0, 0, -10f), VGet(0, 0, 0));

            // fps
            int mStartTime = 0;      //測定開始時刻
            int mCount = 0;          //カウンタ
            float mFps = 0f;          //fps
            const int N = 60;  //平均を取るサンプル数
            const int FPS = 60;  //設定したFPS

            // 移動速度
            int lastTime = GetNowCount();
            const float idouMax = 100;

            // 玉
            bool tamaKeyFlg = false;
            List<VECTOR> tama = new List<VECTOR>();
            tama.Clear();

            // 隕石
            int insekiTime = 0;
            List<VECTOR> inseki = new List<VECTOR>();
            inseki.Clear();

            // 爆発
            int PHandle = LoadGraph("Dead.png");
            List<effect> Bomb = new List<effect>();
            Bomb.Clear();

            while ((ProcessMessage() == 0) && (ClearDrawScreen() == 0) && (CheckHitKey(KEY_INPUT_ESCAPE) != TRUE))
            {
                // fps
                if (mCount == 0)
                { //1フレーム目なら時刻を記憶
                    mStartTime = GetNowCount();
                }
                if (mCount == N)
                { //60フレーム目なら平均を計算する
                    int t = GetNowCount();
                    mFps = 1000.0f / ((t - mStartTime) / (float)N);
                    mCount = 0;
                    mStartTime = t;
                }
                mCount++;

                // 移動速度
                int nowTime = GetNowCount();
                float loopTime = (float)(nowTime - lastTime) / 1000f;
                lastTime = nowTime;

                // 自機の移動
                if (CheckHitKey(KEY_INPUT_LEFT) == TRUE)
                {
                    pos = VSub(pos, VGet(loopTime * 4f, 0, 0));
                }
                if (CheckHitKey(KEY_INPUT_RIGHT) == TRUE)
                {
                    pos = VAdd(pos, VGet(loopTime * 4f, 0f, 0));
                }
                if (CheckHitKey(KEY_INPUT_DOWN) == TRUE)
                {
                    pos = VSub(pos, VGet(0, loopTime * 4f, 0));
                }
                if (CheckHitKey(KEY_INPUT_UP) == TRUE)
                {
                    pos = VAdd(pos, VGet(0, loopTime * 4f, 0));
                }

                // 発射
                if (CheckHitKey(KEY_INPUT_SPACE) == TRUE)
                {
                    if (!tamaKeyFlg)
                    {
                        tamaKeyFlg = true;
                        VECTOR newTama = VSub(pos, VGet(0f, 0f, 1f));
                        tama.Add(newTama);
                    }
                }
                else
                {
                    tamaKeyFlg = false;
                }

                // 玉の移動
                {
                    int i = 0;
                    while (i < tama.Count)
                    {
                        tama[i] = VAdd(tama[i], VGet(0f, 0f, loopTime * 10f));
                        if (tama[i].z > idouMax)
                        {
                            tama.RemoveAt(i);
                        }
                        else
                        {
                            i++;
                        }
                    }
                }

                // 隕石の移動
                insekiTime--;
                if (insekiTime < 0)
                {
                    VECTOR newInseki = VGet(GetRand(16) - 8, GetRand(10) - 5, idouMax);
                    inseki.Add(newInseki);
                    insekiTime = GetRand(50);
                }

                {
                    int i = 0;
                    while (i < inseki.Count)
                    {
                        inseki[i] = VSub(inseki[i], VGet(0f, 0f, loopTime * 50f));
                        if (inseki[i].z < 0f)
                        {
                            inseki.RemoveAt(i);
                        }
                        else
                        {
                            i++;
                        }
                    }
                }

                // 球と球の当たり判定
                {
                    int i = 0;
                    while (i < tama.Count)
                    {
                        bool hit = false;
                        int j = 0;
                        while (j < inseki.Count)
                        {
                            // (x2-x1)^2 + (y2-y1)^2 + (z2-z1)^2 <= (r1+r2)^2
                            float x = (float)Math.Pow(inseki[j].x - tama[i].x, 2);
                            float y = (float)Math.Pow(inseki[j].y - tama[i].y, 2);
                            float z = (float)Math.Pow(inseki[j].z - tama[i].z, 2);
                            if ((x + y + z) <= (float)Math.Pow(1.0f + 0.1f, 2))
                            {
                                hit = true;
                                inseki.RemoveAt(j);
                                break;
                            }
                            else
                            {
                                j++;
                            }
                        }

                        if (hit)
                        {
                            effect eff = new effect();
                            eff.Alpha = 1.0f;
                            eff.Size = 2f;
                            eff.Position = tama[i];
                            Bomb.Add(eff);

                            tama.RemoveAt(i);
                        }
                        else
                        {
                            i++;
                        }
                    }
                }

                // 爆発
                {
                    int i = 0;
                    while (i < Bomb.Count)
                    {
                        effect eff = Bomb[i];
                        eff.Alpha -= (loopTime / 3f);
                        eff.Size += loopTime;
                        Bomb[i] = eff;

                        if (Bomb[i].Alpha < 0.0f)
                        {
                            Bomb.RemoveAt(i);
                        }
                        else
                        {
                            i++;
                        }
                    }
                }

                SetBackgroundColor(160, 216, 239);

                DrawString(0, 0, mFps.ToString(), GetColor(255, 255, 255));

                // 本体表示
                DrawCone3D(
                    VAdd(pos, VGet(0f, 0f, 1f)),
                    VSub(pos, VGet(0f, 0f, 1f)),
                    0.5f,
                    1,
                    GetColor(0, 0, 255),
                    GetColor(0, 255, 0),
                    TRUE);
                // 翼
                DrawTriangle3D(
                    VAdd(pos, VGet(0f, 0f, 1f)),
                    VSub(pos, VGet(1f, 0f, 1f)),
                    VSub(pos, VGet(-1f, 0f, 1f)),
                    GetColor(0, 255, 255),
                    TRUE);

                // 玉
                foreach (VECTOR vec in tama)
                {
                    DrawSphere3D(vec, 0.1f, 1, GetColor(0, 255, 0), GetColor(255, 255, 255), TRUE);
                }

                // 隕石
                foreach (VECTOR vec in inseki)
                {
                    DrawSphere3D(vec, 1f, 1, GetColor(255, 255, 0), GetColor(255, 255, 255), TRUE);
                }

                // 爆発
                SetDrawBlendMode(DX_BLENDMODE_ALPHA, 255);
                SetDrawBright(0, 255, 255); // DrawBillboard3Dの色
                foreach (effect eff in Bomb)
                {
                    SetDrawBlendMode(DX_BLENDMODE_SUB, (int)(eff.Alpha * 255.0f));
                    DrawBillboard3D(eff.Position,
                        0.5f, 0.5f,
                        eff.Size,
                        0.0f,
                        PHandle, TRUE);
                }
                SetDrawBright(255, 255, 255); // DrawBillboard3Dの色
                SetDrawBlendMode(DX_BLENDMODE_NOBLEND, 255);

                // fps
                int tookTime = GetNowCount() - mStartTime;  //かかった時間
                int waitTime = mCount * 1000 / FPS - tookTime;  //待つべき時間
                if (waitTime > 0)
                {
                    Thread.Sleep(waitTime);  //待機
                }

                // 裏画面の内容を表画面に反映させる
                ScreenFlip();
            }

            DxLib_End();				// ＤＸライブラリ使用の終了処理
        }
    }
}
