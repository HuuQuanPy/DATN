using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GradutionThesis
{
    class RobotMath
    {
        private static readonly float piDegree = (float)(Math.PI) / 180;
        private static readonly float L1 = 22.25F;
        private static readonly float L2 = 24F;
        private static readonly float L3 = 19.15F;
        private static readonly float L4 = 15F;

        public static int startAngle1 = 7;
        public static int startAngle2 = 27;
        public static int startAngle3 = 94;
        public static int startAngle4 = 22;
        public static bool errWorkSpace = false;

        public static float[] LocationA1 = { -15, 34.2F, 14.5F };
        public static float[] LocationA2 = { -5, 35.4F, 14.5F };
        public static float[] LocationA3 = {  5, 35.4F, 14.5F };
        public static float[] LocationA4 = { 15, 34.2F, 14.5F };
        public static float[] LocationB1 = { -17, 25, 13.5F };
        public static float[] LocationB2 = { -5, 24.4F, 13.5F };
        public static float[] LocationB3 = {  5, 24.4F, 13.5F };
        public static float[] LocationB4 = { 17, 25F, 13.5F };
        public static float[] LocationC1 = { -17, 15.5F, 13.5F };
        public static float[] LocationC2 = { -5F, 14.8F, 13.5F};
        public static float[] LocationC3 = {  5F, 14.8F, 13.5F };
        public static float[] LocationC4 = {  16, 15.5F, 13.5F };

        public static float[] LocationCam = { -29.587F, 6.832F, 13.142F };
        public static float[] Point_TopLeftCAM = { -22.824F, 14.7F, 14.5F };


        public struct Forward
        {
            public float _theta1;
            public float _theta2;
            public float _theta3;
            public float _theta4;
            //public float _theta5;
        }
        public struct Inverse
        {
            public  float _axisX;
            public  float _axisY;
            public  float _axisZ;
        }
        public static float[] ForwardKinematics(Forward forward)
        {
            float[] arrPos = new float[3];
            arrPos[0] = (float)(Math.Cos(forward._theta1 * piDegree) * (L2 * Math.Cos(forward._theta2 * piDegree) + L3 * Math.Cos(forward._theta2 * piDegree + forward._theta3 * piDegree) + L4 * Math.Cos(forward._theta2 * piDegree + forward._theta3 * piDegree + forward._theta4 * piDegree)));  //Toa do truc X
            arrPos[1] = (float)(Math.Sin(forward._theta1 * piDegree) * (L2 * Math.Cos(forward._theta2 * piDegree) + L3 * Math.Cos(forward._theta2 * piDegree + forward._theta3 * piDegree) + L4 * Math.Cos(forward._theta2 * piDegree + forward._theta3 * piDegree + forward._theta4 * piDegree))); //Toa do truc Y
            arrPos[2] = (float)(L1 + L2 * Math.Sin(forward._theta2 * piDegree) + L3 * Math.Sin(forward._theta2 * piDegree + forward._theta3 * piDegree) + L4 * Math.Sin(forward._theta2 * piDegree + forward._theta3 * piDegree + forward._theta4 * piDegree)); //Toa do truc Z
            return arrPos;
        }
        public static float[] InverseKinematics(Inverse inverse)
        {
            float[] arrTheta = new float[4];
            if (inverse._axisY > 0)
            {
                arrTheta[0] = (float)Math.Atan(inverse._axisY / inverse._axisX) / piDegree; //Gia tri goc Theta1
                if (arrTheta[0] < 0)
                {
                    arrTheta[0] += 180;
                }
            }
            else if(inverse._axisX < 0)
            {
                arrTheta[0] = (float)Math.Atan(inverse._axisY / inverse._axisX) / piDegree + 180;
            }
            
            float Px = (float)Math.Sqrt(Math.Pow(inverse._axisX, 2) + Math.Pow(inverse._axisY, 2));
            float Py = inverse._axisZ - L1 + L4;
            float C3 = (float)(Math.Pow(Px, 2) + Math.Pow(Py, 2) - Math.Pow(L2, 2) - Math.Pow(L3, 2)) / (2 * L2 * L3);
            if (C3 >= -1 && C3 <= 1)
            {
                float S3 = (float)(-Math.Sqrt(1 - Math.Pow(C3, 2)));
                arrTheta[2] = (float)Math.Atan(S3 / C3) / piDegree; // Gia tri goc Theta3
                if (arrTheta[2] > 0)
                {
                    arrTheta[2] -= 180;
                }
                float T2 = ((L2 + L3 * C3) * Py - L3 * S3 * Px) / ((L2 + L3 * C3) * Px + L3 * S3 * Py);
                arrTheta[1] = (float)Math.Atan(T2) / piDegree; // Gia tri goc Theta2
                if (arrTheta[1] < 0)
                {
                    arrTheta[1] += 180;
                }
                arrTheta[3] = -90 - arrTheta[1] - arrTheta[2]; // Gia tri goc Theta4
                errWorkSpace = false;
                return arrTheta;
                
            }
            else
            {
                arrTheta[0] = 45;
                arrTheta[1] = 60;
                arrTheta[2] = -120;
                arrTheta[3] = -90;
                errWorkSpace = true;
                return arrTheta;
            }
        }
    }
}
