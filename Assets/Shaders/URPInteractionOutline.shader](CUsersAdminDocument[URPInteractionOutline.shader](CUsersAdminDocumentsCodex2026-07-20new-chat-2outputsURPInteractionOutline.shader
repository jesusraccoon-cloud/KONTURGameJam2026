Shader "KONTUR/URP/Interaction Outline" // Объявляем отдельный URP-шейдер контура
{
    Properties // Описываем параметры, доступные материалу и C#-скрипту
    {
        [HDR] _OutlineColor("Outline Color", Color) = (4, 0.03, 0.03, 1) // HDR-цвет контура
        _OutlineWidth("Outline Width", Float) = 0.012 // Толщина контура в мировых единицах
    }

    SubShader // Начинаем реализацию шейдера
    {
        Tags // Указываем URP, прозрачную очередь и тип материала
        {
            "RenderPipeline" = "UniversalPipeline" // Разрешаем использовать SubShader только в URP
            "RenderType" = "Transparent" // Помечаем контур как прозрачный эффект
            "Queue" = "Transparent+10" // Рисуем контур после непрозрачной геометрии
        }

        Pass // Единственный проход, который рисует расширенную обратную сторону модели
        {
            Name "InteractionOutline" // Даём проходу понятное имя для Frame Debugger
            Tags { "LightMode" = "SRPDefaultUnlit" } // Используем стандартный непосвещённый проход URP

            Cull Front // Отсекаем передние грани и оставляем расширенные задние грани вокруг силуэта
            ZWrite Off // Контур не изменяет буфер глубины сцены
            ZTest LEqual // Не показываем контур через стены и другие объекты перед ним
            Blend SrcAlpha OneMinusSrcAlpha // Включаем обычное смешивание по прозрачности

            HLSLPROGRAM // Начинаем HLSL-код прохода
            #pragma vertex OutlineVertex // Назначаем функцию обработки вершин
            #pragma fragment OutlineFragment // Назначаем функцию окрашивания пикселей
            #pragma target 2.0 // Сохраняем совместимость с широким набором видеокарт
            #pragma multi_compile_instancing // Добавляем поддержку GPU Instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl" // Подключаем основные функции URP

            CBUFFER_START(UnityPerMaterial) // Начинаем блок параметров конкретного материала
                half4 _OutlineColor; // Получаем HDR-цвет из материала
                float _OutlineWidth; // Получаем толщину из материала
            CBUFFER_END // Заканчиваем блок параметров материала

            struct Attributes // Описываем данные одной вершины исходной модели
            {
                float4 positionOS : POSITION; // Позиция вершины в локальном пространстве объекта
                float3 normalOS : NORMAL; // Нормаль вершины в локальном пространстве объекта
                UNITY_VERTEX_INPUT_INSTANCE_ID // Добавляем идентификатор экземпляра для GPU Instancing
            };

            struct Varyings // Описываем данные, передаваемые от вершины к пикселю
            {
                float4 positionCS : SV_POSITION; // Итоговая позиция вершины на экране
                UNITY_VERTEX_OUTPUT_STEREO // Добавляем совместимость со стереорендерингом
            };

            Varyings OutlineVertex(Attributes input) // Обрабатываем одну вершину модели
            {
                Varyings output; // Создаём результат обработки вершины
                UNITY_SETUP_INSTANCE_ID(input); // Подготавливаем данные GPU Instancing
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output); // Подготавливаем данные стереорендеринга

                float3 positionWS = TransformObjectToWorld(input.positionOS.xyz); // Переводим позицию вершины в мировое пространство
                float3 normalWS = TransformObjectToWorldNormal(input.normalOS); // Переводим нормаль в мировое пространство
                positionWS += normalize(normalWS) * _OutlineWidth; // Расширяем модель наружу вдоль нормали
                output.positionCS = TransformWorldToHClip(positionWS); // Переводим расширенную вершину в экранное пространство
                return output; // Возвращаем подготовленную вершину
            }

            half4 OutlineFragment(Varyings input) : SV_Target // Окрашиваем каждый видимый пиксель контура
            {
                return _OutlineColor; // Возвращаем выбранный HDR-цвет без влияния освещения
            }
            ENDHLSL // Заканчиваем HLSL-код
        }
    }

    FallBack Off // Не используем шейдеры другого Render Pipeline при ошибке
}