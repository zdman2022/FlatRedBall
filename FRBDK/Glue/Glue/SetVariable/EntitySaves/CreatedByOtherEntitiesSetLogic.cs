﻿using FlatRedBall.Glue.Elements;
using FlatRedBall.Glue.FormHelpers;
using FlatRedBall.Glue.Parsing;
using FlatRedBall.Glue.Plugins.EmbeddedPlugins.FactoryPlugin;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Glue.SaveClasses;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GlueFormsCore.SetVariable.EntitySaves
{
    class CreatedByOtherEntitiesSetLogic
    {
        public static void HandleCreatedByOtherEntitiesSet(EntitySave entitySave)
        {
            if (entitySave.CreatedByOtherEntities == true)
            {
                // is this abstract?
                var isAbstract = entitySave.AllNamedObjects.Any(item => item.SetByDerived);

                if(isAbstract)
                {
                    GlueCommands.Self.DialogCommands.ShowMessageBox($"Cannot set {entitySave} to true because this entity contains an object which is SetByDerived.");
                    entitySave.CreatedByOtherEntities = false;
                }
                else
                {
                    FactoryElementCodeGenerator.AddGeneratedPerformanceTypes();
                    FactoryElementCodeGenerator.GenerateAndAddFactoryToProjectClass(entitySave);
                    GlueCommands.Self.ProjectCommands.SaveProjects();
                }
            }
            else
            {
                FactoryElementCodeGenerator.RemoveFactory(entitySave);
                GlueCommands.Self.ProjectCommands.SaveProjects();
            }


            List<EntitySave> entitiesToRefresh = ObjectFinder.Self.GetAllEntitiesThatInheritFrom(entitySave);
            entitiesToRefresh.AddRange(entitySave.GetAllBaseEntities());
            entitiesToRefresh.Add(entitySave);

            // We need to re-generate all objects that use this Entity
            foreach (EntitySave entityToRefresh in entitiesToRefresh)
            {
                List<NamedObjectSave> namedObjects = ObjectFinder.Self.GetAllNamedObjectsThatUseEntity(entityToRefresh.Name);

                foreach (NamedObjectSave nos in namedObjects)
                {
                    var namedObjectContainer = nos.GetContainer();

                    if (namedObjectContainer != null)
                    {
                        CodeWriter.GenerateCode(namedObjectContainer);
                    }
                }
            }
            GlueCommands.Self.RefreshCommands.RefreshPropertyGrid();
        }
    }
}
