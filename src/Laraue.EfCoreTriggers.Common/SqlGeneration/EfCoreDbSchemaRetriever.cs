﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Laraue.EfCoreTriggers.Common.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace Laraue.EfCoreTriggers.Common.SqlGeneration
{
    /// <inheritdoc />
    public class EfCoreDbSchemaRetriever : IDbSchemaRetriever
    {
        /// <summary>
        /// Model used for generating SQL. From this model takes column names, table names and other meta information.
        /// </summary>
        private IModel Model { get; }

        /// <summary>
        /// Initializes a new instance of <see cref="EfCoreDbSchemaRetriever"/>.
        /// </summary>
        /// <param name="model"></param>
        public EfCoreDbSchemaRetriever(IModel model)
        {
            Model = model;
        }
    
        /// <inheritdoc />
        public string GetColumnName(Type type, MemberInfo memberInfo)
        {
            var entityType = GetEntityType(type);
            var column = GetColumn(type, memberInfo);
        
            var identifier = (StoreObjectIdentifier)StoreObjectIdentifier.Create(entityType, StoreObjectType.Table)!;
            return column.GetColumnName(identifier) 
                   ?? throw new InvalidOperationException($"Column information was not found for {identifier}");
        }

        private IProperty GetColumn(Type type, MemberInfo memberInfo)
        {
            return GetEntityType(type).FindProperty(memberInfo.Name)
                   ?? throw new InvalidOperationException($"Column {memberInfo.Name} was not found in {type}");
        }
    
        private IEntityType GetEntityType(Type type)
        {
            var entityType = Model.FindEntityType(type);
            
            if (entityType == null)
            {
                throw new InvalidOperationException($"DbSet<{type}> should be added to the DbContext");
            }

            return entityType;
        }

        /// <inheritdoc />
        public bool IsModel(Type type) =>
            Model.FindEntityType(type) is not null;

        /// <inheritdoc />
        public string GetTableName(Type type)
        {
            return GetEntityType(type).GetTableName()
                   ?? throw new InvalidOperationException($"{type} is not mapped to the table");
        }

        /// <summary>
        /// Get schema name for passed <see cref="Type">ClrType</see>.
        /// </summary>
        /// <param name="entity"></param>
        /// <returns></returns>
        public virtual string? GetTableSchemaName(Type entity)
        {
            return GetTableSchemaName(GetEntityType(entity));
        }

        /// <inheritdoc />
        public string? GetTableSchemaName(IEntityType? entityType)
        {
            return entityType?.GetSchema();
        }

        /// <inheritdoc />
        public PropertyInfo[] GetPrimaryKeyMembers(Type type)
        {
            var entityType = Model.FindEntityType(type);
        
            return entityType
                       ?.FindPrimaryKey()
                       ?.Properties
                       .Select(x => x.PropertyInfo!)
                       .ToArray()
                   ?? Array.Empty<PropertyInfo>();
        }

        /// <inheritdoc />
        public KeyInfo[] GetForeignKeyMembers(Type type, MemberInfo memberInfo)
        {
            IEntityType entityType = Model.FindEntityType(type) ?? throw new ArgumentException($"The type {type} is not a model.");

            IForeignKey outerForeignKey = entityType.FindNavigation(memberInfo)?.ForeignKey 
                ?? throw new ArgumentException($"The member {memberInfo} is not a relation.");

            IReadOnlyList<IProperty> outerKey = outerForeignKey
                .PrincipalKey
                .Properties;

            IReadOnlyList<IProperty> innerKey = outerForeignKey.Properties;

            return outerKey
                .Zip(innerKey, (first, second)
                    => new KeyInfo(
                        first.PropertyInfo ?? throw new NotSupportedException("The key must be a property."),
                        second.PropertyInfo ?? throw new NotSupportedException("The key must be a property.")))
                .ToArray();
        }

        /// <inheritdoc />
        public Type GetActualClrType(Type type, MemberInfo memberInfo)
        {
            var columnType = GetColumn(type, memberInfo);

            return columnType.FindAnnotation("ProviderClrType")?.Value as Type ?? columnType.ClrType;
        }

        /// <inheritdoc />
        public bool IsRelation(Type entity, MemberInfo memberInfo) => 
            IsModel(entity) && GetEntityType(entity).FindNavigation(memberInfo) is not null;

        /// <inheritdoc/>
        public bool ModelsAreCompatible(Type entity1, Type entity2) => 
            IsModel(entity1) && (entity1 == entity2 || (IsModel(entity2) && (
                entity1.IsAssignableFrom(entity2) || entity2.IsAssignableFrom(entity1)) &&
                GetPrimaryKeyMembers(entity1).OrderBy(m => m.Name).SequenceEqual(
                    GetPrimaryKeyMembers(entity2).OrderBy(m => m.Name))));
        
        /// <inheritdoc/>
        public bool CanShortcutRelation(Type entity, MemberInfo relation, out KeyInfo[] foreignKeys, out MemberInfo[] relationPrimarykeys)
        {
            foreignKeys = GetForeignKeyMembers(entity, relation);
            relationPrimarykeys = GetPrimaryKeyMembers(relation.GetResultType());
            return relationPrimarykeys.Length == 1 && foreignKeys.Length == 1 && foreignKeys[0].PrincipalKey == relationPrimarykeys[0];

        }
    }
}