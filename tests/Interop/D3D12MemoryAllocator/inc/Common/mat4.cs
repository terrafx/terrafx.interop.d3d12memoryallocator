// Copyright © Tanner Gooding and Contributors. Licensed under the MIT License (MIT). See License.md in the repository root for more information.

// Ported from D3D12Sample.cpp in D3D12MemoryAllocator commit 5457bcdaee73ee1f3fe6027bbabf959119f88b3d
// Original source is Copyright © Advanced Micro Devices, Inc. All rights reserved. Licensed under the MIT License (MIT).

using System;
using static TerraFX.Interop.UnitTests.D3D12MemAllocTests;

namespace TerraFX.Interop.UnitTests
{
    internal unsafe struct mat4
    {
        public float _11, _12, _13, _14;

        public float _21, _22, _23, _24;

        public float _31, _32, _33, _34;

        public float _41, _42, _43, _44;

        public mat4(float _11, float _12, float _13, float _14,
                    float _21, float _22, float _23, float _24,
                    float _31, float _32, float _33, float _34,
                    float _41, float _42, float _43, float _44)
        {
            this._11 = _11;
            this._12 = _12;
            this._13 = _13;
            this._14 = _14;

            this._21 = _21;
            this._22 = _22;
            this._23 = _23;
            this._24 = _24;

            this._31 = _31;
            this._32 = _32;
            this._33 = _33;
            this._34 = _34;

            this._41 = _41;
            this._42 = _42;
            this._43 = _43;
            this._44 = _44;
        }

        public mat4([NativeTypeName("const vec4&")] in vec4 row1, [NativeTypeName("const vec4&")] in vec4 row2, [NativeTypeName("const vec4&")] in vec4 row3, [NativeTypeName("const vec4&")] in vec4 row4)
        {
            _11 = row1.x;
            _12 = row1.y;
            _13 = row1.z;
            _14 = row1.w;

            _21 = row2.x;
            _22 = row2.y;
            _23 = row2.z;
            _24 = row2.w;

            _31 = row3.x;
            _32 = row3.y;
            _33 = row3.z;
            _34 = row3.w;

            _41 = row4.x;
            _42 = row4.y;
            _43 = row4.z;
            _44 = row4.w;
        }

        public mat4([NativeTypeName("const float*")] float* data)
        {
            _11 = data[0];
            _12 = data[1];
            _13 = data[2];
            _14 = data[3];

            _21 = data[4];
            _22 = data[5];
            _23 = data[6];
            _24 = data[7];

            _31 = data[8];
            _32 = data[9];
            _33 = data[10];
            _34 = data[11];

            _41 = data[12];
            _42 = data[13];
            _43 = data[14];
            _44 = data[15];
        }

        public static mat4 operator *([NativeTypeName("const mat4&")] in mat4 lhs, [NativeTypeName("const mat4&")] in mat4 rhs)
        {
            return new mat4(
                (lhs._11 * rhs._11) + (lhs._12 * rhs._21) + (lhs._13 * rhs._31) + (lhs._14 * rhs._41),
                (lhs._11 * rhs._12) + (lhs._12 * rhs._22) + (lhs._13 * rhs._32) + (lhs._14 * rhs._42),
                (lhs._11 * rhs._13) + (lhs._12 * rhs._23) + (lhs._13 * rhs._33) + (lhs._14 * rhs._43),
                (lhs._11 * rhs._14) + (lhs._12 * rhs._24) + (lhs._13 * rhs._34) + (lhs._14 * rhs._44),

                (lhs._21 * rhs._11) + (lhs._22 * rhs._21) + (lhs._23 * rhs._31) + (lhs._24 * rhs._41),
                (lhs._21 * rhs._12) + (lhs._22 * rhs._22) + (lhs._23 * rhs._32) + (lhs._24 * rhs._42),
                (lhs._21 * rhs._13) + (lhs._22 * rhs._23) + (lhs._23 * rhs._33) + (lhs._24 * rhs._43),
                (lhs._21 * rhs._14) + (lhs._22 * rhs._24) + (lhs._23 * rhs._34) + (lhs._24 * rhs._44),

                (lhs._31 * rhs._11) + (lhs._32 * rhs._21) + (lhs._33 * rhs._31) + (lhs._34 * rhs._41),
                (lhs._31 * rhs._12) + (lhs._32 * rhs._22) + (lhs._33 * rhs._32) + (lhs._34 * rhs._42),
                (lhs._31 * rhs._13) + (lhs._32 * rhs._23) + (lhs._33 * rhs._33) + (lhs._34 * rhs._43),
                (lhs._31 * rhs._14) + (lhs._32 * rhs._24) + (lhs._33 * rhs._34) + (lhs._34 * rhs._44),

                (lhs._41 * rhs._11) + (lhs._42 * rhs._21) + (lhs._43 * rhs._31) + (lhs._44 * rhs._41),
                (lhs._41 * rhs._12) + (lhs._42 * rhs._22) + (lhs._43 * rhs._32) + (lhs._44 * rhs._42),
                (lhs._41 * rhs._13) + (lhs._42 * rhs._23) + (lhs._43 * rhs._33) + (lhs._44 * rhs._43),
                (lhs._41 * rhs._14) + (lhs._42 * rhs._24) + (lhs._43 * rhs._34) + (lhs._44 * rhs._44)
            );
        }

        public static mat4 Identity => new mat4(
            1.0f, 0.0f, 0.0f, 0.0f,
            0.0f, 1.0f, 0.0f, 0.0f,
            0.0f, 0.0f, 1.0f, 0.0f,
            0.0f, 0.0f, 0.0f, 1.0f
        );

        public static mat4 Translation([NativeTypeName("const vec3&")] in vec3 v)
        {
            return new mat4(
                1.0f, 0.0f, 0.0f, 0.0f,
                0.0f, 1.0f, 0.0f, 0.0f,
                0.0f, 0.0f, 1.0f, 0.0f,
                v.x, v.y, v.z, 1.0f
            );
        }

        public static mat4 Scaling(float s)
        {
            return new mat4(
                s, 0.0f, 0.0f, 0.0f,
                0.0f, s, 0.0f, 0.0f,
                0.0f, 0.0f, s, 0.0f,
                0.0f, 0.0f, 0.0f, 1.0f
            );
        }

        public static mat4 Scaling([NativeTypeName("const vec3&")] in vec3 s)
        {
            return new mat4(
                s.x, 0.0f, 0.0f, 0.0f,
                0.0f, s.y, 0.0f, 0.0f,
                0.0f, 0.0f, s.z, 0.0f,
                0.0f, 0.0f, 0.0f, 1.0f
            );
        }

        public static mat4 RotationX(float angle)
        {
            float s = MathF.Sin(angle), c = MathF.Cos(angle);
            return new mat4(
                1.0f, 0.0f, 0.0f, 0.0f,
                0.0f, c, s, 0.0f,
                0.0f, -s, c, 0.0f,
                0.0f, 0.0f, 0.0f, 1.0f
            );
        }

        public static mat4 RotationY(float angle)
        {
            float s = MathF.Sin(angle), c = MathF.Cos(angle);
            return new mat4(
                c, s, 0.0f, 0.0f,
                -s, c, 0.0f, 0.0f,
                0.0f, 0.0f, 1.0f, 0.0f,
                0.0f, 0.0f, 0.0f, 1.0f
            );
        }

        public static mat4 RotationZ(float angle)
        {
            float s = MathF.Sin(angle), c = MathF.Cos(angle);
            return new mat4(
                c, 0.0f, -s, 0.0f,
                0.0f, 1.0f, 0.0f, 0.0f,
                s, 0.0f, c, 0.0f,
                0.0f, 0.0f, 0.0f, 1.0f
            );
        }

        public static mat4 Perspective(float fovY, float aspectRatio, float zNear, float zFar)
        {
            float yScale = 1.0f / MathF.Tan(fovY * 0.5f);
            float xScale = yScale / aspectRatio;
            return new mat4(
                xScale, 0.0f, 0.0f, 0.0f,
                0.0f, yScale, 0.0f, 0.0f,
                0.0f, 0.0f, zFar / (zFar - zNear), 1.0f,
                0.0f, 0.0f, -zNear * zFar / (zFar - zNear), 0.0f
            );
        }

        public static mat4 LookAt(vec3 at, vec3 eye, vec3 up)
        {
            vec3 zAxis = (at - eye).Normalized();
            vec3 xAxis = Cross(up, zAxis).Normalized();
            vec3 yAxis = Cross(zAxis, xAxis);
            return new mat4(
                xAxis.x, yAxis.x, zAxis.x, 0.0f,
                xAxis.y, yAxis.y, zAxis.y, 0.0f,
                xAxis.z, yAxis.z, zAxis.z, 0.0f,
                -Dot(xAxis, eye), -Dot(yAxis, eye), -Dot(zAxis, eye), 1.0f
            );
        }

        public readonly mat4 Transposed()
        {
            return new mat4(
                _11, _21, _31, _41,
                _12, _22, _32, _42,
                _13, _23, _33, _43,
                _14, _24, _34, _44
            );
        }
    }
}
