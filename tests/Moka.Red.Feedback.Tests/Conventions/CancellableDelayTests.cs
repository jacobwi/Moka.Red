using System.Reflection;
using System.Reflection.Emit;

namespace Moka.Red.Feedback.Tests.Conventions;

public class CancellableDelayTests
{
	private const BindingFlags Everything = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public |
	                                        BindingFlags.NonPublic | BindingFlags.DeclaredOnly;

	private static readonly Dictionary<short, OpCode> OpCodesByValue = typeof(OpCodes)
		.GetFields(BindingFlags.Public | BindingFlags.Static)
		.Select(field => (OpCode)field.GetValue(null)!)
		.ToDictionary(code => code.Value);

	// Components cut many of their waits short: the pointer leaves before a sub-menu opens, a second copy replaces the
	// first one's reset. Task.Delay with a token ends such a wait by throwing, a first-chance exception each time, which
	// a debugger or a trace reports as an error. Wait on an ITimer from the TimeProvider and dispose it instead, with a
	// number that tells a callback already on its way that it's been superseded (MokaSearchInput). The scan reads the
	// compiled packages, so async methods, lambdas and Razor code blocks are all covered, and AsyncVoidTests checks
	// that every package under src is among them.
	[Fact]
	public void NoPackage_CallsTaskDelayWithAToken()
	{
		string[] offenders = AsyncVoidTests.PackageAssemblies()
			.SelectMany(assembly => assembly.GetTypes())
			.SelectMany(type => type.GetMethods(Everything).Concat<MethodBase>(type.GetConstructors(Everything)))
			.Where(method => Calls(method).Any(IsCancellableDelay))
			.Select(method => $"{method.DeclaringType!.FullName}.{method.Name}")
			.ToArray();

		Assert.Empty(offenders);
	}

	[Fact]
	public void TheScan_TellsADelayWithATokenFromOneWithout()
	{
		Assert.Contains(Calls(Sample(nameof(WaitWithAToken))), IsCancellableDelay);
		Assert.DoesNotContain(Calls(Sample(nameof(WaitWithoutAToken))), IsCancellableDelay);
	}

	private static Task WaitWithAToken(CancellationToken token) => Task.Delay(1, token);

	private static Task WaitWithoutAToken() => Task.Delay(1);

	private static MethodInfo Sample(string name) => typeof(CancellableDelayTests).GetMethod(name, Everything)!;

	private static bool IsCancellableDelay(MethodBase callee) =>
		callee.DeclaringType == typeof(Task)
		&& callee.Name == nameof(Task.Delay)
		&& callee.GetParameters().Any(parameter => parameter.ParameterType == typeof(CancellationToken));

	private static IEnumerable<MethodBase> Calls(MethodBase method)
	{
		byte[]? il = method.GetMethodBody()?.GetILAsByteArray();
		if (il is null)
		{
			yield break;
		}

		Type[]? typeArguments = method.DeclaringType is { IsGenericType: true } type ? type.GetGenericArguments() : null;
		Type[]? methodArguments = method is MethodInfo { IsGenericMethod: true } ? method.GetGenericArguments() : null;

		for (int at = 0; at < il.Length;)
		{
			OpCode code = il[at] == 0xFE ? OpCodesByValue[unchecked((short)(0xFE00 | il[at + 1]))] : OpCodesByValue[il[at]];
			at += code.Size;
			if (code.OperandType == OperandType.InlineMethod &&
			    method.Module.ResolveMethod(BitConverter.ToInt32(il, at), typeArguments, methodArguments) is { } callee)
			{
				yield return callee;
			}

			at += OperandSize(code.OperandType, il, at);
		}
	}

	private static int OperandSize(OperandType operand, byte[] il, int at) => operand switch
	{
		OperandType.InlineNone => 0,
		OperandType.ShortInlineBrTarget or OperandType.ShortInlineI or OperandType.ShortInlineVar => 1,
		OperandType.InlineVar => 2,
		OperandType.InlineI8 or OperandType.InlineR => 8,
		OperandType.InlineSwitch => 4 + 4 * BitConverter.ToInt32(il, at),
		_ => 4
	};
}
