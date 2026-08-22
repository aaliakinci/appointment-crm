using System.Reflection;
using AppointmentCrm.Api.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;

namespace AppointmentCrm.UnitTests.Controllers;

public sealed class ControllerResponseContractTests
{
    private static readonly Type[] ControllerTypes = typeof(AuthController)
        .Assembly
        .GetExportedTypes()
        .Where(type =>
            !type.IsAbstract
            && typeof(ControllerBase).IsAssignableFrom(type))
        .OrderBy(type => type.FullName, StringComparer.Ordinal)
        .ToArray();

    [Fact]
    public void Controllers_AreDiscoveredAndDeclareApiControllerBehavior()
    {
        Assert.NotEmpty(ControllerTypes);
        Assert.All(
            ControllerTypes,
            controllerType => Assert.NotNull(
                controllerType.GetCustomAttribute<ApiControllerAttribute>()));
    }

    [Fact]
    public void HttpActions_UseTypedContractsMatchingTheirSuccessMetadata()
    {
        foreach (MethodInfo action in ControllerTypes.SelectMany(GetHttpActions))
        {
            Type actionResultType = UnwrapTask(action.ReturnType);
            ProducesResponseTypeAttribute success = Assert.Single(
                action.GetCustomAttributes<ProducesResponseTypeAttribute>(),
                response => response.StatusCode is >= 200 and < 300);

            if (actionResultType == typeof(ActionResult))
            {
                Assert.Equal(StatusCodes.Status204NoContent, success.StatusCode);
                continue;
            }

            Assert.True(
                actionResultType.IsGenericType
                    && actionResultType.GetGenericTypeDefinition() == typeof(ActionResult<>),
                $"{action.DeclaringType?.Name}.{action.Name} must return ActionResult<T>. ");
            Assert.Equal(actionResultType.GenericTypeArguments[0], success.Type);
        }
    }

    [Fact]
    public void Controllers_DeclareTheCommonProblemResponses()
    {
        foreach (Type controllerType in ControllerTypes)
        {
            ProducesResponseTypeAttribute[] responses = controllerType
                .GetCustomAttributes<ProducesResponseTypeAttribute>()
                .ToArray();

            AssertProblemResponse(
                responses,
                StatusCodes.Status500InternalServerError);

            if (controllerType.GetCustomAttribute<AuthorizeAttribute>() is not null)
            {
                AssertProblemResponse(responses, StatusCodes.Status401Unauthorized);
                AssertProblemResponse(responses, StatusCodes.Status403Forbidden);
            }
        }
    }

    private static void AssertProblemResponse(
        IEnumerable<ProducesResponseTypeAttribute> responses,
        int statusCode)
    {
        ProducesResponseTypeAttribute response = Assert.Single(
            responses,
            candidate => candidate.StatusCode == statusCode);
        Assert.Equal(typeof(ProblemDetails), response.Type);
    }

    private static IEnumerable<MethodInfo> GetHttpActions(Type controllerType) =>
        controllerType
            .GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .Where(method => method.GetCustomAttributes<HttpMethodAttribute>().Any());

    private static Type UnwrapTask(Type returnType) =>
        returnType.IsGenericType && returnType.GetGenericTypeDefinition() == typeof(Task<>)
            ? returnType.GenericTypeArguments[0]
            : returnType;
}
