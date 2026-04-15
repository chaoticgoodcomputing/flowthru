using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.Optimization;

namespace Flowthru.Misc.ML.UMAP.Core;

/// <summary>
/// Helper for computing UMAP curve fitting parameters.
/// </summary>
/// <remarks>
/// <para>
/// UMAP uses a smooth curve to approximate the attractive force between points:
/// </para>
/// <code>
/// weight(dist) = 1 / (1 + a * dist^(2b))
/// </code>
/// <para>
/// Parameters <c>a</c> and <c>b</c> are fit to match an exponential decay curve
/// based on the <c>spread</c> and <c>min_dist</c> hyperparameters:
/// </para>
/// <list type="bullet">
///   <item><description>For dist &lt; min_dist: weight = 1.0 (fully connected)</description></item>
///   <item><description>For dist ≥ min_dist: weight = exp(-(dist - min_dist) / spread)</description></item>
/// </list>
/// <para>
/// Python UMAP reference: <c>find_ab_params()</c> in <c>umap_.py</c> (lines 1393-1408)
/// </para>
/// <para>
/// This implementation uses the Levenberg-Marquardt algorithm to match Python's
/// scipy.optimize.curve_fit behavior, ensuring identical parameter values.
/// </para>
/// </remarks>
public static class CurveFitting
{
    /// <summary>
    /// Computes curve parameters a and b from spread and min_dist using curve fitting.
    /// </summary>
    /// <param name="spread">Effective scale of embedded points.</param>
    /// <param name="minDist">Minimum distance between embedded points.</param>
    /// <returns>Tuple of (a, b) parameters.</returns>
    public static (float a, float b) FindABParams(float spread, float minDist)
    {
        Console.WriteLine($"[CurveFitting] Computing a,b for spread={spread}, minDist={minDist}");

        const int nSamples = 300;
        var maxX = spread * 3;

        // Generate sample points
        var xValues = new double[nSamples];
        var yValues = new double[nSamples];

        for (var i = 0; i < nSamples; i++)
        {
            var x = i * maxX / (nSamples - 1);
            xValues[i] = x;

            // Target curve: piecewise with exponential decay
            if (x < minDist)
            {
                yValues[i] = 1.0;
            }
            else
            {
                yValues[i] = Math.Exp(-(x - minDist) / spread);
            }
        }

        // Fit curve: y = 1 / (1 + a * x^(2b))
        // Use Levenberg-Marquardt nonlinear least squares (matches Python scipy.optimize.curve_fit)
        var (a, b) = FitCurve(xValues, yValues);

        Console.WriteLine($"[CurveFitting] Fitted parameters: a={a:F6}, b={b:F6}");
        return ((float)a, (float)b);
    }

    /// <summary>
    /// Fits the UMAP curve to the target exponential decay using Levenberg-Marquardt.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This method uses the Levenberg-Marquardt algorithm to minimize the sum of squared
    /// residuals between the target values and the model function:
    /// </para>
    /// <code>
    /// model(x; a, b) = 1 / (1 + a * x^(2b))
    /// </code>
    /// <para>
    /// The Levenberg-Marquardt algorithm is the same optimizer used by Python's
    /// scipy.optimize.curve_fit, ensuring consistent results across implementations.
    /// </para>
    /// </remarks>
    private static (double a, double b) FitCurve(double[] xValues, double[] yValues)
    {
        // Convert to vectors for Math.NET Numerics
        var xVector = Vector<double>.Build.DenseOfArray(xValues);
        var yVector = Vector<double>.Build.DenseOfArray(yValues);

        // Model function: f(x; a, b) = 1 / (1 + a * x^(2b))
        // Returns vector of model predictions for all x values
        Vector<double> ModelFunction(Vector<double> parameters, Vector<double> x)
        {
            var a = parameters[0];
            var b = parameters[1];
            var result = Vector<double>.Build.Dense(x.Count);

            for (var i = 0; i < x.Count; i++)
            {
                var xi = x[i];
                // Avoid numerical issues when x is very small
                if (xi < 1e-10)
                {
                    result[i] = 1.0;
                }
                else
                {
                    var denominator = 1.0 + a * Math.Pow(xi, 2 * b);
                    result[i] = 1.0 / denominator;
                }
            }

            return result;
        }

        // Initial guess for parameters (Python scipy uses [1, 1] by default)
        var initialGuess = Vector<double>.Build.DenseOfArray([1.0, 1.0]);

        try
        {
            // Create objective function for nonlinear model fitting
            // accuracyOrder=6 uses 6th order finite differences for Jacobian approximation
            var objective = ObjectiveFunction.NonlinearModel(
              ModelFunction,
              xVector,
              yVector,
              accuracyOrder: 6
            );

            // Use Levenberg-Marquardt minimizer (same as Python scipy.optimize.curve_fit)
            var solver = new LevenbergMarquardtMinimizer();
            var result = solver.FindMinimum(objective, initialGuess);

            // Extract fitted parameters
            var a = result.MinimizingPoint[0];
            var b = result.MinimizingPoint[1];

            // Check if optimization succeeded
            // RelativePoints: step size became smaller than tolerance (converged)
            // Converged: explicit convergence flag
            // Both indicate successful optimization
            var isSuccess =
              result.ReasonForExit == ExitCondition.Converged
              || result.ReasonForExit == ExitCondition.RelativePoints
              || result.ReasonForExit == ExitCondition.RelativeGradient;

            // Validate the results are reasonable
            if (a > 0 && b > 0 && a < 100.0 && b < 10.0 && isSuccess)
            {
                return (a, b);
            }

            // If parameters are out of reasonable range or didn't converge, use defaults
            Console.WriteLine(
              $"[CurveFitting] WARNING: Fitted parameters out of range or failed to converge "
                + $"(a={a:F6}, b={b:F6}, exit={result.ReasonForExit}), using defaults"
            );
            return (1.577, 0.895); // Updated defaults matching Python output
        }
        catch (Exception ex)
        {
            // Fallback to reasonable defaults if fitting fails
            Console.WriteLine(
              $"[CurveFitting] WARNING: Curve fitting failed ({ex.Message}), using defaults"
            );
            return (1.577, 0.895); // Updated defaults matching Python output
        }
    }
}
