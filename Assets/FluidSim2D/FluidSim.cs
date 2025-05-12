using System.Collections;
using UnityEngine;

namespace FluidSim2DProject
{

    public class FluidSim : MonoBehaviour
    {
        [Header("시뮬레이션 설정")]
        public Color m_fluidColor = new Color(0, 0, 1, 1); // 파란색 열변색 물감
        public Color m_obstacleColor = new Color(0.8f, 0.8f, 0.8f, 1); // 비커 색상
        public float m_impulseTemperature = 10.0f; // 초기 온도
        public float m_impulseDensity = 1.0f; // 초기 밀도
        public float m_temperatureDissipation = 0.99f; // 온도 소산
        public float m_velocityDissipation = 0.99f; // 속도 소산
        public float m_densityDissipation = 0.9999f; // 밀도 소산
        public float m_ambientTemperature = 0.0f; // 주변 온도
        public float m_smokeBuoyancy = 1.0f; // 부력
        public float m_smokeWeight = 0.05f; // 연기 무게

        [Header("비커 설정")]
        public float m_beakerWidth = 0.6f; // 비커 너비
        public float m_beakerHeight = 0.8f; // 비커 높이
        public float m_beakerWallThickness = 0.02f; // 비커 벽 두께

        public Material m_guiMat, m_advectMat, m_buoyancyMat, m_divergenceMat, m_jacobiMat, m_impluseMat, m_gradientMat, m_obstaclesMat;

        RenderTexture m_guiTex, m_divergenceTex, m_obstaclesTex;
        RenderTexture[] m_velocityTex, m_densityTex, m_pressureTex, m_temperatureTex;

        float m_cellSize = 1.0f;
        float m_gradientScale = 1.0f;

        Vector2 m_inverseSize;
        int m_numJacobiIterations = 50;

        Vector2 m_implusePos = new Vector2(0.5f, 0.0f);
        float m_impluseRadius = 0.1f;
        float m_mouseImpluseRadius = 0.05f;

        Vector2 m_obstaclePos = new Vector2(0.5f, 0.5f);
        float m_obstacleRadius = 0.1f;

        Rect m_rect;
        int m_width, m_height;
 
        void Start()
        {
            m_width = 512;
            m_height = 512;

            Vector2 size = new Vector2(m_width, m_height);
            Vector2 pos = new Vector2(Screen.width / 2, Screen.height / 2) - size * 0.5f;
            m_rect = new Rect(pos, size);

            m_inverseSize = new Vector2(1.0f / m_width, 1.0f / m_height);

            m_velocityTex = new RenderTexture[2];
            m_densityTex = new RenderTexture[2];
            m_temperatureTex = new RenderTexture[2];
            m_pressureTex = new RenderTexture[2];

            CreateSurface(m_velocityTex, RenderTextureFormat.RGFloat, FilterMode.Bilinear);
            CreateSurface(m_densityTex, RenderTextureFormat.RFloat, FilterMode.Bilinear);
            CreateSurface(m_temperatureTex, RenderTextureFormat.RFloat, FilterMode.Bilinear);
            CreateSurface(m_pressureTex, RenderTextureFormat.RFloat, FilterMode.Point);

            m_guiTex = new RenderTexture(m_width, m_height, 0, RenderTextureFormat.ARGB32);
            m_guiTex.filterMode = FilterMode.Bilinear;
            m_guiTex.wrapMode = TextureWrapMode.Clamp;
            m_guiTex.Create();

            m_divergenceTex = new RenderTexture(m_width, m_height, 0, RenderTextureFormat.RFloat, RenderTextureReadWrite.Linear);
            m_divergenceTex.filterMode = FilterMode.Point;
            m_divergenceTex.wrapMode = TextureWrapMode.Clamp;
            m_divergenceTex.Create();

            m_obstaclesTex = new RenderTexture(m_width, m_height, 0, RenderTextureFormat.RFloat, RenderTextureReadWrite.Linear);
            m_obstaclesTex.filterMode = FilterMode.Point;
            m_obstaclesTex.wrapMode = TextureWrapMode.Clamp;
            m_obstaclesTex.Create();

            // 초기 열변색 물감 주입
            Vector2 initialPos = new Vector2(0.5f, 0.2f); // 비커 하단
            ApplyImpulse(m_temperatureTex[0], m_temperatureTex[1], initialPos, 0.05f, m_impulseTemperature);
            ApplyImpulse(m_densityTex[0], m_densityTex[1], initialPos, 0.05f, m_impulseDensity);
        }

        void OnGUI()
        {
            GUI.DrawTexture(m_rect, m_guiTex);
        }

        void CreateSurface(RenderTexture[] surface, RenderTextureFormat format, FilterMode filter)
        {
            surface[0] = new RenderTexture(m_width, m_height, 0, format, RenderTextureReadWrite.Linear);
            surface[0].filterMode = filter;
            surface[0].wrapMode = TextureWrapMode.Clamp;
            surface[0].Create();

            surface[1] = new RenderTexture(m_width, m_height, 0, format, RenderTextureReadWrite.Linear);
            surface[1].filterMode = filter;
            surface[1].wrapMode = TextureWrapMode.Clamp;
            surface[1].Create();
        }

        void Advect(RenderTexture velocity, RenderTexture source, RenderTexture dest, float dissipation, float timeStep)
        {
            m_advectMat.SetVector("_InverseSize", m_inverseSize);
            m_advectMat.SetFloat("_TimeStep", timeStep);
            m_advectMat.SetFloat("_Dissipation", dissipation);
            m_advectMat.SetTexture("_Velocity", velocity);
            m_advectMat.SetTexture("_Source", source);
            m_advectMat.SetTexture("_Obstacles", m_obstaclesTex);

            Graphics.Blit(null, dest, m_advectMat);
        }

        void ApplyBuoyancy(RenderTexture velocity, RenderTexture temperature, RenderTexture density, RenderTexture dest, float timeStep)
        {
            m_buoyancyMat.SetTexture("_Velocity", velocity);
            m_buoyancyMat.SetTexture("_Temperature", temperature);
            m_buoyancyMat.SetTexture("_Density", density);
            m_buoyancyMat.SetFloat("_AmbientTemperature", m_ambientTemperature);
            m_buoyancyMat.SetFloat("_TimeStep", timeStep);
            m_buoyancyMat.SetFloat("_Sigma", m_smokeBuoyancy);
            m_buoyancyMat.SetFloat("_Kappa", m_smokeWeight);

            Graphics.Blit(null, dest, m_buoyancyMat);
        }

        void ApplyImpulse(RenderTexture source, RenderTexture dest, Vector2 pos, float radius, float val)
        {
            m_impluseMat.SetVector("_Point", pos);
            m_impluseMat.SetFloat("_Radius", radius);
            m_impluseMat.SetFloat("_Fill", val);
            m_impluseMat.SetTexture("_Source", source);

            Graphics.Blit(null, dest, m_impluseMat);
        }

        void ComputeDivergence(RenderTexture velocity, RenderTexture dest)
        {
            m_divergenceMat.SetFloat("_HalfInverseCellSize", 0.5f / m_cellSize);
            m_divergenceMat.SetTexture("_Velocity", velocity);
            m_divergenceMat.SetVector("_InverseSize", m_inverseSize);
            m_divergenceMat.SetTexture("_Obstacles", m_obstaclesTex);

            Graphics.Blit(null, dest, m_divergenceMat);
        }

        void Jacobi(RenderTexture pressure, RenderTexture divergence, RenderTexture dest)
        {

            m_jacobiMat.SetTexture("_Pressure", pressure);
            m_jacobiMat.SetTexture("_Divergence", divergence);
            m_jacobiMat.SetVector("_InverseSize", m_inverseSize);
            m_jacobiMat.SetFloat("_Alpha", -m_cellSize * m_cellSize);
            m_jacobiMat.SetFloat("_InverseBeta", 0.25f);
            m_jacobiMat.SetTexture("_Obstacles", m_obstaclesTex);

            Graphics.Blit(null, dest, m_jacobiMat);
        }

        void SubtractGradient(RenderTexture velocity, RenderTexture pressure, RenderTexture dest)
        {
            m_gradientMat.SetTexture("_Velocity", velocity);
            m_gradientMat.SetTexture("_Pressure", pressure);
            m_gradientMat.SetFloat("_GradientScale", m_gradientScale);
            m_gradientMat.SetVector("_InverseSize", m_inverseSize);
            m_gradientMat.SetTexture("_Obstacles", m_obstaclesTex);

            Graphics.Blit(null, dest, m_gradientMat);
        }

        void AddObstacles()
        {
            m_obstaclesMat.SetVector("_InverseSize", m_inverseSize);
            
            // 비커 모양 그리기
            float leftWall = 0.5f - m_beakerWidth * 0.5f;
            float rightWall = 0.5f + m_beakerWidth * 0.5f;
            float bottomWall = 0.1f;
            float topWall = bottomWall + m_beakerHeight;

            // 왼쪽 벽
            m_obstaclesMat.SetVector("_Point", new Vector2(leftWall, 0.5f));
            m_obstaclesMat.SetFloat("_Radius", m_beakerWallThickness);
            Graphics.Blit(null, m_obstaclesTex, m_obstaclesMat);

            // 오른쪽 벽
            m_obstaclesMat.SetVector("_Point", new Vector2(rightWall, 0.5f));
            Graphics.Blit(m_obstaclesTex, m_obstaclesTex, m_obstaclesMat);

            // 바닥
            m_obstaclesMat.SetVector("_Point", new Vector2(0.5f, bottomWall));
            m_obstaclesMat.SetFloat("_Radius", m_beakerWidth * 0.5f);
            Graphics.Blit(m_obstaclesTex, m_obstaclesTex, m_obstaclesMat);
        }

        void ClearSurface(RenderTexture surface)
        {
            Graphics.SetRenderTarget(surface);
            GL.Clear(false, true, new Color(0, 0, 0, 0));
            Graphics.SetRenderTarget(null);
        }

        void Swap(RenderTexture[] texs)
        {
            RenderTexture temp = texs[0];
            texs[0] = texs[1];
            texs[1] = temp;
        }

        void FixedUpdate()
        {
            //Obstacles only need to be added once unless changed.
            AddObstacles();

            //Set the density field and obstacle color.
            m_guiMat.SetColor("_FluidColor", m_fluidColor);
            m_guiMat.SetColor("_ObstacleColor", m_obstacleColor);
            m_guiMat.SetTexture("_Temperature", m_temperatureTex[0]);

            int READ = 0;
            int WRITE = 1;
            float dt = 0.125f;

            //Advect velocity against its self
            Advect(m_velocityTex[READ], m_velocityTex[READ], m_velocityTex[WRITE], m_velocityDissipation, dt);
            //Advect temperature against velocity
            Advect(m_velocityTex[READ], m_temperatureTex[READ], m_temperatureTex[WRITE], m_temperatureDissipation, dt);
            //Advect density against velocity
            Advect(m_velocityTex[READ], m_densityTex[READ], m_densityTex[WRITE], m_densityDissipation, dt);

            Swap(m_velocityTex);
            Swap(m_temperatureTex);
            Swap(m_densityTex);

            //Determine how the flow of the fluid changes the velocity
            ApplyBuoyancy(m_velocityTex[READ], m_temperatureTex[READ], m_densityTex[READ], m_velocityTex[WRITE], dt);

            Swap(m_velocityTex);

            //Refresh the impluse of density and temperature
            ApplyImpulse(m_temperatureTex[READ], m_temperatureTex[WRITE], m_implusePos, m_impluseRadius, m_impulseTemperature);
            ApplyImpulse(m_densityTex[READ], m_densityTex[WRITE], m_implusePos, m_impluseRadius, m_impulseDensity);

            Swap(m_temperatureTex);
            Swap(m_densityTex);

            //If left click down add impluse, if right click down remove impulse from mouse pos.
            if(Input.GetMouseButton(0) || Input.GetMouseButton(1))
            {
                Vector2 pos = Input.mousePosition;

                pos.x -= m_rect.xMin;
                pos.y -= m_rect.yMin;

                pos.x /= m_rect.width;
                pos.y /= m_rect.height;

                float sign = (Input.GetMouseButton(0)) ? 1.0f : -1.0f;

                ApplyImpulse(m_temperatureTex[READ], m_temperatureTex[WRITE], pos, m_mouseImpluseRadius, m_impulseTemperature);
                ApplyImpulse(m_densityTex[READ], m_densityTex[WRITE], pos, m_mouseImpluseRadius, m_impulseDensity * sign);

                Swap(m_temperatureTex);
                Swap(m_densityTex);
            }

            //Calculates how divergent the velocity is
            ComputeDivergence(m_velocityTex[READ], m_divergenceTex);

            ClearSurface(m_pressureTex[READ]);

            int i = 0;
            for (i = 0; i < m_numJacobiIterations; ++i)
            {
                Jacobi(m_pressureTex[READ], m_divergenceTex, m_pressureTex[WRITE]);
                Swap(m_pressureTex);
            }

            //Use the pressure tex that was last rendered into. This computes divergence free velocity
            SubtractGradient(m_velocityTex[READ], m_pressureTex[READ], m_velocityTex[WRITE]);

            Swap(m_velocityTex);

            //Render the tex you want to see into gui tex. Will only use the red channel
            m_guiMat.SetTexture("_Obstacles", m_obstaclesTex);
            Graphics.Blit(m_densityTex[READ], m_guiTex, m_guiMat);
        }
    }

}
