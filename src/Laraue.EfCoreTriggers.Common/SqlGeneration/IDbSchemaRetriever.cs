﻿using System;
using System.Reflection;
using Microsoft.EntityFrameworkCore.Metadata;

namespace Laraue.EfCoreTriggers.Common.SqlGeneration
{
    /// <summary>
    /// The adapter allows to retrieve DB metadata about entity.
    /// </summary>
    public interface IDbSchemaRetriever
    {
        /// <summary>
        /// Get the column name of the passed member.
        /// Note: this is just a column name, without table name, quotes and schema.
        /// </summary>
        /// <param name="type">Entity type.</param>
        /// <param name="memberInfo">Member to get.</param>
        /// <returns></returns>
        string GetColumnName(Type type, MemberInfo memberInfo);
    
        /// <summary>
        /// Get the table name of passed entity.
        /// Note: this is just a table name, without quotes and schema.
        /// </summary>
        /// <param name="entity"></param>
        /// <returns></returns>
        string GetTableName(Type entity);

        /// <summary>
        /// Checks whether the provided type is a model type
        /// </summary>
        /// <param name="entity"></param>
        /// <returns></returns>
        bool IsModel(Type entity);
    
        /// <summary>
        /// Checks whether the provided member is a relation on a model type
        /// </summary>
        /// <param name="memberInfo"></param>
        /// <returns></returns>
        bool IsRelation(Type entity, MemberInfo memberInfo);

        /// <summary>
        /// Checks whether the primary keys are the same for two model types
        /// </summary>
        /// <param name="entity1"></param>
        /// <param name="entity2"></param>
        /// <returns></returns>
        bool ModelsAreCompatible(Type entity1, Type entity2);

        /// <summary>
        /// Return schema name for the passed entity if it is exists.
        /// </summary>
        /// <param name="entity"></param>
        /// <returns></returns>
        string? GetTableSchemaName(Type entity);
    
        /// <summary>
        /// Return schema name for the passed entity if it is exists.
        /// </summary>
        /// <param name="entityType"></param>
        /// <returns></returns>
        string? GetTableSchemaName(IEntityType entityType);

        /// <summary>
        /// Get all members which are used in primary key.
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        PropertyInfo[] GetPrimaryKeyMembers(Type type);

        /// <summary>
        /// Get info about cases participating in relations between two types.
        /// </summary>
        /// <param name="type"></param>
        /// <param name="memberInfo"></param>
        /// <returns></returns>
        KeyInfo[] GetForeignKeyMembers(Type type, MemberInfo memberInfo);

        /// <summary>
        /// Determines whether accessing the primary key of the related entity can be shortcuted by using the foreign key id
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="relation"></param>
        /// <param name="foreignKeys"></param>
        /// <param name="relationPrimarykeys"></param>
        /// <returns></returns>
        bool CanShortcutRelation(Type entity, MemberInfo relation, out KeyInfo[] foreignKeys, out MemberInfo[] relationPrimarykeys);

        /// <summary>
        /// Some type can be overriden, for example Enum can be store as string in the DB.
        /// In these cases clr type will be returned from this function.
        /// </summary>
        /// <param name="type">Entity type.</param>
        /// <param name="memberInfo">Entity member.</param>
        /// <returns></returns>
        Type GetActualClrType(Type type, MemberInfo memberInfo);
    }
}