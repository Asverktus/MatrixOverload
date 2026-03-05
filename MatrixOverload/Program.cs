using System;
using System.Text;

namespace MatrixCalculator
{
  public class MatrixException : Exception
  {
    public MatrixException(string message) : base(message) { }
  }

  public class MatrixDimensionMismatchException : MatrixException
  {
    public MatrixDimensionMismatchException(string operation)
      : base($"Matrix dimension mismatch for operation: {operation}") { }
  }

  public class SingularMatrixException : MatrixException
  {
    public SingularMatrixException(string operation)
      : base($"Cannot perform {operation} on singular matrix (determinant is zero)") { }
  }

  public class MatrixIndexOutOfRangeException : MatrixException
  {
    public MatrixIndexOutOfRangeException(int row, int col, int size)
      : base($"Index [{row},{col}] out of range for matrix of size {size}x{size}") { }
  }

  public enum MenuOption
  {
    GenerateMatrices = 1,
    AddMatrices = 2,
    SubtractMatrices = 3,
    MultiplyMatrices = 4,
    MultiplyByScalar = 5,
    CalculateDeterminant = 6,
    FindInverse = 7,
    CompareMatrices = 8,
    ShowMatrices = 9,
    TestExceptions = 10,
    DemonstratePrototype = 11,
    Exit = 0
  }

  public class SquareMatrix : IComparable<SquareMatrix>, IEquatable<SquareMatrix>, ICloneable
  {
    private const int NextRowOffset = 1;
    private const int MinorSizeOffset = 1;
    private const int HashCodeMultiplier = 31;
    private const int LastElementOffset = 1;

    private double[,] _elements;
    private int _size;
    private double? _cachedDeterminant;
    private SquareMatrix _cachedInverse;

    public int size
    {
      get
      {
        return _size;
      }
    }

    public double this[int row, int col]
    {
      get
      {
        if (row < 0 || row >= _size || col < 0 || col >= _size)
        {
          throw new MatrixIndexOutOfRangeException(row, col, _size);
        }
        return _elements[row, col];
      }

      set
      {
        if (row < 0 || row >= _size || col < 0 || col >= _size)
        {
          throw new MatrixIndexOutOfRangeException(row, col, _size);
        }

        _elements[row, col] = value;
        _cachedDeterminant = null;
        _cachedInverse = null;
      }
    }

    public SquareMatrix() : this(2) { }

    public SquareMatrix(int matrixSize)
    {
      if (matrixSize <= 0)
      {
        throw new MatrixException("Matrix size must be positive");
      }

      _size = matrixSize;
      _elements = new double[matrixSize, matrixSize];
      _cachedDeterminant = null;
      _cachedInverse = null;
    }

    public SquareMatrix(SquareMatrix other)
    {
      if (other == null)
      {
        throw new MatrixException("Cannot copy from null matrix");
      }

      _size = other._size;
      _elements = new double[_size, _size];

      for (int row = 0; row < _size; ++row)
      {
        for (int col = 0; col < _size; ++col)
        {
          _elements[row, col] = other._elements[row, col];
        }
      }

      _cachedDeterminant = other._cachedDeterminant;
      _cachedInverse = other._cachedInverse?.Clone() as SquareMatrix;
    }

    public SquareMatrix(int matrixSize, double minValue, double maxValue) : this(matrixSize)
    {
      Random randomGenerator;
      randomGenerator = new Random();

      for (int row = 0; row < _size; ++row)
      {
        for (int col = 0; col < _size; ++col)
        {
          _elements[row, col] = minValue + (maxValue - minValue) * randomGenerator.NextDouble();
        }
      }
    }

    public SquareMatrix(double[,] elements)
    {
      if (elements == null)
      {
        throw new MatrixException("Element array cannot be null");
      }

      if (elements.GetLength(0) != elements.GetLength(1))
      {
        throw new MatrixException("Array must be square");
      }

      _size = elements.GetLength(0);
      _elements = new double[_size, _size];

      for (int row = 0; row < _size; ++row)
      {
        for (int col = 0; col < _size; ++col)
        {
          _elements[row, col] = elements[row, col];
        }
      }

      _cachedDeterminant = null;
      _cachedInverse = null;
    }

    public object Clone()
    {
      return new SquareMatrix(this);
    }

    public SquareMatrix Copy()
    {
      return (SquareMatrix)Clone();
    }

    public static SquareMatrix Identity(int matrixSize)
    {
      SquareMatrix identityMatrix;
      identityMatrix = new SquareMatrix(matrixSize);

      for (int index = 0; index < matrixSize; ++index)
      {
        identityMatrix[index, index] = 1.0;
      }

      return identityMatrix;
    }

    public static SquareMatrix operator +(SquareMatrix left, SquareMatrix right)
    {
      if (left == null || right == null)
      {
        throw new MatrixException("Cannot add null matrices");
      }

      if (left._size != right._size)
      {
        throw new MatrixDimensionMismatchException("addition");
      }

      SquareMatrix resultMatrix;
      resultMatrix = new SquareMatrix(left._size);

      for (int row = 0; row < left._size; ++row)
      {
        for (int col = 0; col < left._size; ++col)
        {
          resultMatrix[row, col] = left[row, col] + right[row, col];
        }
      }

      return resultMatrix;
    }

    public static SquareMatrix operator -(SquareMatrix left, SquareMatrix right)
    {
      if (left == null || right == null)
      {
        throw new MatrixException("Cannot subtract null matrices");
      }

      if (left._size != right._size)
      {
        throw new MatrixDimensionMismatchException("subtraction");
      }

      SquareMatrix resultMatrix;
      resultMatrix = new SquareMatrix(left._size);

      for (int row = 0; row < left._size; ++row)
      {
        for (int col = 0; col < left._size; ++col)
        {
          resultMatrix[row, col] = left[row, col] - right[row, col];
        }
      }

      return resultMatrix;
    }

    public static SquareMatrix operator *(SquareMatrix left, SquareMatrix right)
    {
      if (left == null || right == null)
      {
        throw new MatrixException("Cannot multiply null matrices");
      }

      if (left._size != right._size)
      {
        throw new MatrixDimensionMismatchException("multiplication");
      }

      SquareMatrix resultMatrix;
      resultMatrix = new SquareMatrix(left._size);

      for (int row = 0; row < left._size; ++row)
      {
        for (int col = 0; col < left._size; ++col)
        {
          double sum;
          sum = 0.0;

          for (int inner = 0; inner < left._size; ++inner)
          {
            sum += left[row, inner] * right[inner, col];
          }

          resultMatrix[row, col] = sum;
        }
      }

      return resultMatrix;
    }

    public static SquareMatrix operator *(SquareMatrix matrix, double scalar)
    {
      if (matrix == null)
      {
        throw new MatrixException("Cannot multiply null matrix");
      }

      SquareMatrix resultMatrix;
      resultMatrix = new SquareMatrix(matrix._size);

      for (int row = 0; row < matrix._size; ++row)
      {
        for (int col = 0; col < matrix._size; ++col)
        {
          resultMatrix[row, col] = matrix[row, col] * scalar;
        }
      }

      return resultMatrix;
    }

    public static SquareMatrix operator *(double scalar, SquareMatrix matrix)
    {
      return matrix * scalar;
    }

    public static SquareMatrix operator /(SquareMatrix matrix, double scalar)
    {
      if (matrix == null)
      {
        throw new MatrixException("Cannot divide null matrix");
      }

      if (Math.Abs(scalar) < 1e-10)
      {
        throw new MatrixException("Division by zero");
      }

      SquareMatrix resultMatrix;
      resultMatrix = new SquareMatrix(matrix._size);

      for (int row = 0; row < matrix._size; ++row)
      {
        for (int col = 0; col < matrix._size; ++col)
        {
          resultMatrix[row, col] = matrix[row, col] / scalar;
        }
      }

      return resultMatrix;
    }

    public static bool operator >(SquareMatrix left, SquareMatrix right)
    {
      if (left == null || right == null)
      {
        return false;
      }

      return left.CompareTo(right) > 0;
    }

    public static bool operator <(SquareMatrix left, SquareMatrix right)
    {
      if (left == null || right == null)
      {
        return false;
      }

      return left.CompareTo(right) < 0;
    }

    public static bool operator >=(SquareMatrix left, SquareMatrix right)
    {
      if (left == null || right == null)
      {
        return false;
      }

      return left.CompareTo(right) >= 0;
    }

    public static bool operator <=(SquareMatrix left, SquareMatrix right)
    {
      if (left == null || right == null)
      {
        return false;
      }

      return left.CompareTo(right) <= 0;
    }

    public static bool operator ==(SquareMatrix left, SquareMatrix right)
    {
      if (ReferenceEquals(left, null) && ReferenceEquals(right, null))
      {
        return true;
      }

      if (ReferenceEquals(left, null) || ReferenceEquals(right, null))
      {
        return false;
      }

      return left.Equals(right);
    }

    public static bool operator !=(SquareMatrix left, SquareMatrix right)
    {
      return !(left == right);
    }

    public static bool operator true(SquareMatrix matrix)
    {
      if (matrix == null)
      {
        return false;
      }

      for (int row = 0; row < matrix._size; ++row)
      {
        for (int col = 0; col < matrix._size; ++col)
        {
          if (Math.Abs(matrix[row, col]) < 1e-10)
          {
            return false;
          }
        }
      }

      return true;
    }

    public static bool operator false(SquareMatrix matrix)
    {
      if (matrix == null)
      {
        return true;
      }

      for (int row = 0; row < matrix._size; ++row)
      {
        for (int col = 0; col < matrix._size; ++col)
        {
          if (Math.Abs(matrix[row, col]) < 1e-10)
          {
            return true;
          }
        }
      }

      return false;
    }

    public static explicit operator double[,](SquareMatrix matrix)
    {
      if (matrix == null)
      {
        throw new MatrixException("Cannot convert null matrix to array");
      }

      double[,] resultArray;
      resultArray = new double[matrix._size, matrix._size];

      for (int row = 0; row < matrix._size; ++row)
      {
        for (int col = 0; col < matrix._size; ++col)
        {
          resultArray[row, col] = matrix[row, col];
        }
      }

      return resultArray;
    }

    public static implicit operator SquareMatrix(double[,] array)
    {
      return new SquareMatrix(array);
    }

    public double determinant
    {
      get
      {
        if (_cachedDeterminant.HasValue)
        {
          return _cachedDeterminant.Value;
        }

        double[,] tempMatrix;
        tempMatrix = new double[_size, _size];

        for (int row = 0; row < _size; ++row)
        {
          for (int col = 0; col < _size; ++col)
          {
            tempMatrix[row, col] = _elements[row, col];
          }
        }

        double determinantValue;
        determinantValue = 1.0;

        int sign;
        sign = 1;

        for (int column = 0; column < _size; ++column)
        {
          int pivotRow;
          pivotRow = -1;

          for (int row = column; row < _size; ++row)
          {
            if (Math.Abs(tempMatrix[row, column]) > 1e-10)
            {
              pivotRow = row;
              break;
            }
          }

          if (pivotRow == -1)
          {
            _cachedDeterminant = 0.0;
            return 0.0;
          }

          if (pivotRow != column)
          {
            for (int col = 0; col < _size; ++col)
            {
              double temp;
              temp = tempMatrix[column, col];
              tempMatrix[column, col] = tempMatrix[pivotRow, col];
              tempMatrix[pivotRow, col] = temp;
            }

            sign = -sign;
          }

          double pivotValue;
          pivotValue = tempMatrix[column, column];
          determinantValue *= pivotValue;

          for (int row = column + NextRowOffset; row < _size; ++row)
          {
            double factor;
            factor = tempMatrix[row, column] / pivotValue;

            for (int col = column; col < _size; ++col)
            {
              tempMatrix[row, col] -= factor * tempMatrix[column, col];
            }
          }
        }

        _cachedDeterminant = determinantValue * sign;
        return _cachedDeterminant.Value;
      }
    }

    public SquareMatrix inverse
    {
      get
      {
        if (_cachedInverse != null)
        {
          return _cachedInverse;
        }

        double det;
        det = determinant;

        if (Math.Abs(det) < 1e-10)
        {
          throw new SingularMatrixException("finding inverse");
        }

        SquareMatrix inverseMatrix;
        inverseMatrix = new SquareMatrix(_size);

        for (int index = 0; index < _size; ++index)
        {
          for (int depth = 0; depth < _size; ++depth)
          {
            SquareMatrix minor;
            minor = new SquareMatrix(_size - MinorSizeOffset);

            for (int row = 0, minorRow = 0; row < _size; ++row)
            {
              if (row == index) continue;
              for (int col = 0, minorCol = 0; col < _size; ++col)
              {
                if (col == depth) continue;
                minor._elements[minorRow, minorCol] = _elements[row, col];
                ++minorCol;
              }
              ++minorRow;
            }

            double minorDet;
            minorDet = minor.determinant;

            double sign;
            sign = ((index + depth) % 2 == 0) ? 1.0 : -1.0;

            inverseMatrix._elements[depth, index] = sign * minorDet / det;
          }
        }

        _cachedInverse = inverseMatrix;
        return inverseMatrix;
      }
    }

    public int CompareTo(SquareMatrix other)
    {
      if (other == null)
      {
        return 1;
      }

      double thisDeterminant;
      thisDeterminant = determinant;

      double otherDeterminant;
      otherDeterminant = other.determinant;

      if (Math.Abs(thisDeterminant - otherDeterminant) < 1e-10)
      {
        return 0;
      }

      if (thisDeterminant < otherDeterminant)
      {
        return -1;
      }

      return 1;
    }

    public override bool Equals(object obj)
    {
      return Equals(obj as SquareMatrix);
    }

    public bool Equals(SquareMatrix other)
    {
      if (other == null)
      {
        return false;
      }

      if (_size != other._size)
      {
        return false;
      }

      for (int row = 0; row < _size; ++row)
      {
        for (int col = 0; col < _size; ++col)
        {
          if (Math.Abs(_elements[row, col] - other._elements[row, col]) > 1e-10)
          {
            return false;
          }
        }
      }

      return true;
    }

    public override int GetHashCode()
    {
      int hashCode;
      hashCode = _size.GetHashCode();

      for (int row = 0; row < Math.Min(_size, 3); ++row)
      {
        for (int col = 0; col < Math.Min(_size, 3); ++col)
        {
          hashCode = hashCode * HashCodeMultiplier + _elements[row, col].GetHashCode();
        }
      }

      return hashCode;
    }

    public override string ToString()
    {
      StringBuilder stringBuilder;
      stringBuilder = new StringBuilder();

      stringBuilder.AppendLine($"Square Matrix {_size}x{_size}:");

      for (int row = 0; row < _size; ++row)
      {
        stringBuilder.Append("[");

        for (int col = 0; col < _size; ++col)
        {
          stringBuilder.Append($" {_elements[row, col],8:F4}");

          if (col < _size - LastElementOffset)
          {
            stringBuilder.Append(",");
          }
        }

        stringBuilder.AppendLine(" ]");
      }

      return stringBuilder.ToString();
    }

    public string ToString(string format)
    {
      if (format == "brief")
      {
        return $"SquareMatrix {_size}x{_size}, det = {determinant:F4}";
      }

      return ToString();
    }
  }

  class Program
  {
    private static SquareMatrix _currentMatrixA;
    private static SquareMatrix _currentMatrixB;

    static void Main(string[] commandLineArgs)
    {
      Console.WriteLine("=== MATRIX CALCULATOR ===\n");

      try
      {
        _currentMatrixA = new SquareMatrix(3, -10.0, 10.0);
        _currentMatrixB = new SquareMatrix(3, -5.0, 5.0);

        Console.WriteLine(
          "Default matrices created:\n" +
          "\nMatrix A:" +
          _currentMatrixA +
          "\nMatrix B:" +
          _currentMatrixB
        );

        RunInteractiveMenu();
      }

      catch (Exception exception)
      {
        Console.WriteLine($"Unexpected error: {exception.Message}");
      }
    }

    static void RunInteractiveMenu()
    {
      while (true)
      {
        Console.WriteLine(
          "\n=== MATRIX CALCULATOR MENU ===\n" +
          "1. Generate new random matrices\n" +
          "2. Add matrices (A + B)\n" +
          "3. Subtract matrices (A - B)\n" +
          "4. Multiply matrices (A * B)\n" +
          "5. Multiply by scalar\n" +
          "6. Calculate determinant\n" +
          "7. Find inverse matrix\n" +
          "8. Compare matrices\n" +
          "9. Show matrices\n" +
          "10. Test exceptions\n" +
          "11. Demonstrate prototype pattern (copy matrix)\n" +
          "0. Exit"
        );

        Console.Write("Choose action: ");
        string userChoiceInput;
        userChoiceInput = Console.ReadLine();

        int userChoice;
        bool parseResult;

        parseResult = int.TryParse(userChoiceInput, out userChoice);

        if (!parseResult)
        {
          Console.WriteLine("Invalid input. Please enter a number.");
          continue;
        }

        if (userChoice == (int)MenuOption.Exit)
        {
          Console.WriteLine("Exiting Matrix Calculator. Goodbye!");
          return;
        }

        try
        {
          MenuOption selectedOption;
          selectedOption = (MenuOption)userChoice;

          switch (selectedOption)
          {
            case MenuOption.GenerateMatrices:
              GenerateRandomMatrices();
              break;

            case MenuOption.AddMatrices:
              PerformAddition();
              break;

            case MenuOption.SubtractMatrices:
              PerformSubtraction();
              break;

            case MenuOption.MultiplyMatrices:
              PerformMultiplication();
              break;

            case MenuOption.MultiplyByScalar:
              PerformScalarMultiplication();
              break;

            case MenuOption.CalculateDeterminant:
              CalculateDeterminant();
              break;

            case MenuOption.FindInverse:
              FindInverse();
              break;

            case MenuOption.CompareMatrices:
              CompareTwoMatrices();
              break;

            case MenuOption.ShowMatrices:
              ShowMatrices();
              break;

            case MenuOption.TestExceptions:
              TestExceptions();
              break;

            case MenuOption.DemonstratePrototype:
              DemonstratePrototype();
              break;

            default:
              Console.WriteLine("Invalid choice. Please try again.");
              break;
          }
        }

        catch (Exception exception)
        {
          Console.WriteLine($"Error: {exception.Message}");
        }
      }
    }

    static void GenerateRandomMatrices()
    {
      Console.Write("Enter matrix size: ");
      string sizeInput;
      sizeInput = Console.ReadLine();

      int matrixSize;
      bool parseResult;

      parseResult = int.TryParse(sizeInput, out matrixSize);

      if (!parseResult || matrixSize <= 0)
      {
        Console.WriteLine("Invalid size. Using default size 3.");
        matrixSize = 3;
      }

      Console.Write("Enter min value: ");
      string minInput;
      minInput = Console.ReadLine();

      double minValue;
      parseResult = double.TryParse(minInput, out minValue);

      if (!parseResult)
      {
        Console.WriteLine("Invalid min value. Using -10.");
        minValue = -10.0;
      }

      Console.Write("Enter max value: ");
      string maxInput;
      maxInput = Console.ReadLine();

      double maxValue;
      parseResult = double.TryParse(maxInput, out maxValue);

      if (!parseResult || maxValue <= minValue)
      {
        Console.WriteLine("Invalid max value. Using 10.");
        maxValue = 10.0;
      }

      _currentMatrixA = new SquareMatrix(matrixSize, minValue, maxValue);
      _currentMatrixB = new SquareMatrix(matrixSize, minValue, maxValue);

      Console.WriteLine(
        "\nGenerated Matrix A:" +
        _currentMatrixA +
        "\nGenerated Matrix B:" +
        _currentMatrixB
      );
    }

    static void PerformAddition()
    {
      if (_currentMatrixA == null || _currentMatrixB == null)
      {
        Console.WriteLine("Matrices not initialized.");
        return;
      }

      Console.WriteLine("\nA + B =");
      Console.WriteLine(_currentMatrixA + _currentMatrixB);
    }

    static void PerformSubtraction()
    {
      if (_currentMatrixA == null || _currentMatrixB == null)
      {
        Console.WriteLine("Matrices not initialized.");
        return;
      }

      Console.WriteLine("\nA - B =");
      Console.WriteLine(_currentMatrixA - _currentMatrixB);
    }

    static void PerformMultiplication()
    {
      if (_currentMatrixA == null || _currentMatrixB == null)
      {
        Console.WriteLine("Matrices not initialized.");
        return;
      }

      Console.WriteLine("\nA * B =");
      Console.WriteLine(_currentMatrixA * _currentMatrixB);
    }

    static void PerformScalarMultiplication()
    {
      if (_currentMatrixA == null)
      {
        Console.WriteLine("Matrix A not initialized.");
        return;
      }

      Console.Write("Enter scalar value: ");
      string scalarInput;
      scalarInput = Console.ReadLine();

      double scalarValue;
      bool parseResult;

      parseResult = double.TryParse(scalarInput, out scalarValue);

      if (!parseResult)
      {
        Console.WriteLine("Invalid scalar. Using 2.0");
        scalarValue = 2.0;
      }

      Console.WriteLine($"\nA * {scalarValue} =");
      Console.WriteLine(_currentMatrixA * scalarValue);
    }

    static void CalculateDeterminant()
    {
      if (_currentMatrixA == null)
      {
        Console.WriteLine("Matrix A not initialized.");
        return;
      }

      Console.WriteLine(
        $"\ndet(A) = {_currentMatrixA.determinant:F6}\n" +
        $"det(B) = {_currentMatrixB.determinant:F6}"
      );
    }

    static void FindInverse()
    {
      if (_currentMatrixA == null)
      {
        Console.WriteLine("Matrix A not initialized.");
        return;
      }

      Console.Write("Inverse of which matrix? (A/B): ");
      string choice;
      choice = Console.ReadLine();

      try
      {
        string lowerCaseChoice;
        lowerCaseChoice = choice.ToLower();

        if (lowerCaseChoice == "a")
        {
          Console.WriteLine(
            "\nA^(-1) =" +
            _currentMatrixA.inverse +
            "\nA * A^(-1) =" +
            (_currentMatrixA * _currentMatrixA.inverse)
          );
        }

        else if (lowerCaseChoice == "b")
        {
          Console.WriteLine("\nB^(-1) =" + _currentMatrixB.inverse);
        }

        else
        {
          Console.WriteLine("Invalid choice.");
        }
      }

      catch (SingularMatrixException exception)
      {
        Console.WriteLine($"Cannot invert matrix: {exception.Message}");
      }
    }

    static void CompareTwoMatrices()
    {
      if (_currentMatrixA == null || _currentMatrixB == null)
      {
        Console.WriteLine("Matrices not initialized.");
        return;
      }

      Console.WriteLine(
        $"\nA == B: {_currentMatrixA == _currentMatrixB}\n" +
        $"A != B: {_currentMatrixA != _currentMatrixB}\n" +
        $"A > B: {_currentMatrixA > _currentMatrixB}\n" +
        $"A < B: {_currentMatrixA < _currentMatrixB}\n" +
        $"A >= B: {_currentMatrixA >= _currentMatrixB}\n" +
        $"A <= B: {_currentMatrixA <= _currentMatrixB}"
      );
    }

    static void ShowMatrices()
    {
      if (_currentMatrixA == null || _currentMatrixB == null)
      {
        Console.WriteLine("Matrices not initialized.");
        return;
      }

      Console.WriteLine("\nMatrix A:" + _currentMatrixA + "\nMatrix B:" + _currentMatrixB);
    }

    static void TestExceptions()
    {
      Console.WriteLine("\n=== TESTING EXCEPTIONS ===");

      try
      {
        Console.WriteLine("1. Testing invalid index access...");
        double value;
        value = _currentMatrixA[10, 10];
      }

      catch (MatrixIndexOutOfRangeException exception)
      {
        Console.WriteLine($"   Caught: {exception.Message}");
      }

      try
      {
        Console.WriteLine("\n2. Testing dimension mismatch...");
        SquareMatrix smallMatrix;
        smallMatrix = new SquareMatrix(2);
        SquareMatrix result;
        result = smallMatrix + _currentMatrixA;
      }

      catch (MatrixDimensionMismatchException exception)
      {
        Console.WriteLine($"   Caught: {exception.Message}");
      }

      try
      {
        Console.WriteLine("\n3. Testing invalid matrix size...");
        SquareMatrix invalidMatrix;
        invalidMatrix = new SquareMatrix(0);
      }

      catch (MatrixException exception)
      {
        Console.WriteLine($"   Caught: {exception.Message}");
      }

      try
      {
        Console.WriteLine("\n4. Testing division by zero...");
        SquareMatrix result;
        result = _currentMatrixA / 0;
      }

      catch (MatrixException exception)
      {
        Console.WriteLine($"   Caught: {exception.Message}");
      }
    }

    static void DemonstratePrototype()
    {
      if (_currentMatrixA == null)
      {
        Console.WriteLine("Matrix A not initialized.");
        return;
      }

      SquareMatrix copy;
      copy = _currentMatrixA.Copy();

      Console.WriteLine(
        "\n=== PROTOTYPE PATTERN DEMO ===\n" +
        "Original matrix A:" +
        _currentMatrixA +
        "\nCopy of matrix A:" +
        copy +
        "\nModified original A[0,0] = 999.999" +
        "\nOriginal A after modification:"
      );

      _currentMatrixA[0, 0] = 999.999;
      Console.WriteLine(_currentMatrixA + "\nCopy (should be unchanged):" + copy);
    }
  }
}