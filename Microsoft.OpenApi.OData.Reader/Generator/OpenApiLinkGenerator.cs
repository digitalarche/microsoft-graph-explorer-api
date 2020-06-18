﻿// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// ------------------------------------------------------------

using System.Collections.Generic;
using Microsoft.OData.Edm;
using Microsoft.OpenApi.OData.Common;
using Microsoft.OpenApi.Models;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.OData.Edm;
using System.Linq;

namespace Microsoft.OpenApi.OData.Generator
{
    /// <summary>
    /// Extension methods to create <see cref="OpenApiLink"/> by <see cref="IEdmModel"/>.
    /// </summary>
    internal static class OpenApiLinkGenerator
    {
        /// <summary>
        /// Create the collection of <see cref="OpenApiLink"/> object.
        /// </summary>
        /// <param name="context">The OData context.</param>
        /// <param name="entityType">The Entity type.</param>
        /// <param name ="sourceElementName">The name of the source of the <see cref="IEdmEntityType"/> object.</param>
        /// <param name="sourceElementType">"The type of the source of the <see cref="IEdmEntityType"/> object.</param>
        /// <param name="parameters">"The list of parameters from the incoming operation.</param>
        /// <param name="declaringEntityTypeName">Optional parameter: Name of the Entity type that declares a Navigation property.</param>
        /// <param name="targetMultiplicity">"Optional parameter: Flag indicating whether the source of the  <see cref="IEdmEntityType"/> object is a collection."</param>
        /// <returns>The created dictionary of <see cref="OpenApiLink"/> object.</returns>
        public static IDictionary<string, OpenApiLink> CreateLinks(this ODataContext context,
            IEdmEntityType entityType, string sourceElementName, string sourceElementType,
            IList<OpenApiParameter> parameters, string declaringEntityTypeName = null,
            bool targetMultiplicity = false)
        {
            IDictionary<string, OpenApiLink> links = new Dictionary<string, OpenApiLink>();

            if (!targetMultiplicity)
            {
                Utils.CheckArgumentNull(context, nameof(context));
                Utils.CheckArgumentNull(entityType, nameof(entityType));
                Utils.CheckArgumentNullOrEmpty(sourceElementName, nameof(sourceElementName));
                Utils.CheckArgumentNullOrEmpty(sourceElementType, nameof(sourceElementType));
                Utils.CheckArgumentNull(parameters, nameof(parameters));

                List<string> pathKeyNames = new List<string>();

                // Fetch defined Id(s) from url path of operation
                foreach (var parameter in parameters)
                {
                    if (!string.IsNullOrEmpty(parameter.Description) &&
                        parameter.Description.ToLower().Contains("key"))
                    {
                        pathKeyNames.Add(parameter.Name);
                    }
                }

                foreach (IEdmNavigationProperty navProp in entityType.NavigationProperties())
                {
                    IEdmEntityType navPropEntity = navProp.ToEntityType();
                    var navPropTypeName = navProp.ToEntityType().Name;
                    string tag;
                    string navPropName = navProp.Name;
                    string operationId;

                    string operationPrefix;

                    switch (sourceElementType)
                    {
                        case "Navigation":   // Just for contained navigations
                            operationPrefix = declaringEntityTypeName + "." + sourceElementName;
                            break;
                        default: // EntitySet, Entity, Singleton
                            operationPrefix = sourceElementName;
                            break;
                    }

                    if (navProp.TargetMultiplicity() == EdmMultiplicity.Many)
                    {
                        operationId = operationPrefix + ".List" + Utils.UpperFirstChar(navPropName);
                    }
                    else
                    {
                        operationId = operationPrefix + ".Get" + Utils.UpperFirstChar(navPropName);
                    }

                    OpenApiLink link = new OpenApiLink
                    {
                        OperationId = operationId,
                        Parameters = new Dictionary<string, RuntimeExpressionAnyWrapper>()
                    };

                    if (pathKeyNames.Any())
                    {
                        foreach (var pathKeyName in pathKeyNames)
                        {
                            link.Parameters[pathKeyName] = new RuntimeExpressionAnyWrapper
                            {
                                Any = new OpenApiString("$request.path." + pathKeyName)
                            };
                        }
                    }

                    links[navProp.Name] = link;
                }
            }

            return links;
        }
    }
}
