﻿namespace VehicleGadgetsPlus
{
    using Rage;

    using VehicleGadgetsPlus.Memory;

    internal unsafe class VehicleBone
    {
        private readonly Vehicle vehicle;
        private readonly int index;
        private readonly fragInstGta* inst;

        public Vehicle Vehicle => vehicle;
        public int Index => index;
        public Matrix Matrix
        {
            get => inst->CacheEntry->Skeleton->ObjectMatrices[index];
            set => inst->CacheEntry->Skeleton->ObjectMatrices[index] = value;
        }

        public Vector3 OriginalTranslation { get; }
        public Quaternion OriginalRotation { get; }

        private VehicleBone(Vehicle vehicle, int index)
        {
            this.vehicle = vehicle;
            this.index = index;

            CVehicle* veh = ((CVehicle*)vehicle.MemoryAddress);
            inst = veh->Inst;

            OriginalTranslation = Util.GetBoneOriginalTranslation(vehicle, index);
            OriginalRotation = Util.GetBoneOriginalRotation(vehicle, index);
        }

        public void RotateAxis(Vector3 axis, float degrees)
        {
            NativeMatrix4x4* matrix = &(inst->CacheEntry->Skeleton->ObjectMatrices[index]);
            Matrix newMatrix = Matrix.Scaling(1.0f, 1.0f, 1.0f) * Matrix.RotationAxis(axis, MathHelper.ConvertDegreesToRadians(degrees)) * (*matrix);
            *matrix = newMatrix;
        }

        public void Translate(Vector3 translation)
        {
            NativeMatrix4x4* matrix = &(inst->CacheEntry->Skeleton->ObjectMatrices[index]);
            Matrix newMatrix = Matrix.Scaling(1.0f, 1.0f, 1.0f) * Matrix.Translation(translation) * (*matrix);
            *matrix = newMatrix;
        }

        public void SetRotation(Quaternion rotation)
        {
            NativeMatrix4x4* matrix = &(inst->CacheEntry->Skeleton->ObjectMatrices[index]);
            MatrixUtils.Decompose(*matrix, out Vector3 scale, out _, out Vector3 translation);
            Matrix newMatrix = Matrix.Scaling(scale) * Matrix.RotationQuaternion(rotation) * Matrix.Translation(translation);
            *matrix = newMatrix;
        }

        public void SetTranslation(Vector3 translation)
        {
            NativeMatrix4x4* matrix = &(inst->CacheEntry->Skeleton->ObjectMatrices[index]);
            matrix->M41 = translation.X;
            matrix->M42 = translation.Y;
            matrix->M43 = translation.Z;
        }

        public void ResetRotation() => SetRotation(OriginalRotation);
        public void ResetTranslation() => SetTranslation(OriginalTranslation);


        public static bool TryGetForVehicle(Vehicle vehicle, string boneName, out VehicleBone bone)
        {
            int boneIndex = Util.GetBoneIndex(vehicle, boneName);
            if (boneIndex == -1)
            {
                bone = null;
                return false;
            }

            bone = new VehicleBone(vehicle, boneIndex);
            return true;
        }
    }
}
