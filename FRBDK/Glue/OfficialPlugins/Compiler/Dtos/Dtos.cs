﻿using FlatRedBall.Glue.SaveClasses;
using System;
using System.Collections.Generic;
using System.Text;

namespace OfficialPlugins.Compiler.Dtos
{
    class RemoveObjectDto
    {
        public string ElementNameGlue { get; set; }
        public string ObjectName { get; set; }
    }

    public class SetVariableDto
    {
        public string InstanceOwner { get; set; }

        public string ObjectName { get; set; }
        public string VariableName { get; set; }
        public object VariableValue { get; set; }
        public string Type { get; set; }
    }

    class SetEditMode
    {
        public bool IsInEditMode { get; set; }
    }

    class SelectObjectDto
    {
        public string ObjectName { get; set; }
        public string ElementName { get; set; }
    }

    public enum AssignOrRecordOnly
    {
        Assign,
        RecordOnly
    }

    public class GlueVariableSetData
    {
        public AssignOrRecordOnly AssignOrRecordOnly { get; set; }
        /// <summary>
        /// The owner of the NamedObjectSave, which is either the current screen or the current entity save
        /// </summary>
        public string InstanceOwnerGameType { get; set; }
        public string VariableName { get; set; }
        public string VariableValue { get; set; }
        public string Type { get; set; }
        public bool IsState { get; set; }
    }

    public class GlueVariableSetDataResponse
    {
        public string Exception { get; set; }
        public bool WasVariableAssigned { get; set; }
    }

    public class GetCameraPosition
    {
        // no members I think...
    }

    public class GetCameraPositionResponse
    {
        public float X { get; set; }
        public float Y { get; set; }
        public float Z { get; set; }
    }

    public class AddObjectDto : NamedObjectSave
    {
        public string ElementNameGame { get; set; }
    }

    public class AddObjectDtoResponse
    {
        public bool WasObjectCreated { get; set; }
    }

    public class MoveObjectToContainerDto
    {
        public string ElementName { get; set; }
        public string ObjectName { get; set; }
        public string ContainerName { get; set; }
    }

    public class MoveObjectToContainerDtoResponse
    {
        public bool WasObjectMoved { get; set; }
    }

    public class RemoveObjectDtoResponse
    {
        public bool WasObjectRemoved { get; set; }
        public bool DidScreenMatch { get; set; }
    }

    public class SetCameraPositionDto
    {
        public Microsoft.Xna.Framework.Vector3 Position { get; set; }
    }

    public class CreateNewEntityDto
    {
        public EntitySave EntitySave { get; set; }
    }

    public class CreateNewStateDto
    {
        public StateSave StateSave { get; set; }
        public string CategoryName { get; set; }
        public string ElementNameGame { get; set; }
    }

    public class ChangeStateVariableDto
    {
        public StateSave StateSave { get; set; }
        public string CategoryName { get; set; }
        public string ElementNameGame { get; set; }
        public string VariableName { get; set; }
    }

    public class RestartScreenDto { }
    public class ReloadGlobalContentDto
    {
        public string StrippedGlobalContentFileName { get; set; }
    }
    public class TogglePauseDto { }
    public class AdvanceOneFrameDto { }
    public class SetSpeedDto
    {
        public int SpeedPercentage { get; set; }
    }
}
