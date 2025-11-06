using System;

class Polynomial
{
    private int degree;
    private double[] coeffs;

    public Polynomial()
    {
        degree = 0;
        coeffs = new double[1] { 0.0 };
    }

    public Polynomial(double[] new_coeffs)
    {
        degree = new_coeffs.Length - 1;
        coeffs = (double[])new_coeffs.Clone();
    }

    public int Degree
    {
        get { return degree; }
    }

    public double[] Coeffs
    {
        get { return (double[])coeffs.Clone(); }
    }

public override string ToString()
{
    if (coeffs == null || coeffs.Length == 0)
        return "0";

    System.Text.StringBuilder result = new System.Text.StringBuilder();

    for (int i = 0; i < coeffs.Length; i++)
    {
        double coef = coeffs[i];

        if (result.Length > 0)
        {
            result.Append(coef > 0 ? " + " : " - ");
        }
        else if (coef < 0)
        {
            result.Append("-");
        }

        double absCoef = Math.Abs(coef);
        if (i == 0 || absCoef != 1)
        {
            result.Append(absCoef);
        }

        if (i > 0)
        {
            result.Append("x");
            if (i > 1)
            {
                result.Append("^").Append(i);
            }
        }
    }

    return result.ToString();
}
    


class Programm
{
    static void Main(string[] args)
    {
        double[] coeffs = { 1.0, 0.0, 2.0 };
        Polynomial p = new Polynomial(coeffs); // 1 + 2x^2

        Console.WriteLine(p);
    }
}
