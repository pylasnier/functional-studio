# functional-studio

IDE for functional programming. A-Level Computer Science NEA

NEA Documentation [here](https://github.com/pylasnier/fs-documentation).

## Getting started

To run, download the bin folder and open `Functional Studio.exe`. On Linux, run the executable through Wine.

To start writing a program, create a new file or open a file (file extension \*.ps). All of the example functions provided in this document are available as examples in the examples folder.

To interface with the program, you must define a variable `main`; this cannot be a function. Give it a type signature, assign it a value, and the output terminal will show you its value when you run the program. Examples for `main` are `int main = 42`, `float main = 3.14`, or `bool main = true`.

Please note that the interpreter is sensitive to newlines and the IDE does not feature wrapping or horizontal scrolling; if a line is getting too long, break it with a `\` followed immediately by a newline and the interpreter will ignore the line break.

## Writing your own function

To write your own function, assign it an appropriate type signature. For example, if we want to define a function that takes a number, adds 1 to it, and returns that new number, it will have a type signature of `int -> int`; it takes an `int` and returns an `int`.

The built-in addition function for integers is `int -> int -> int Add a b`, so to produce an output adding together the numbers 4 and 5 would be `int main = Add 4 5`. Note that math operators don't have infix alternatives, so `Add 4 5` cannot be replaced with ``4 `Add` 5`` or `4 + 5`. The same applies to all of the other available built-in operators.

Our own increment function will then be `int -> int Increment n = Add n 1`. You can test this function by passing it different values, for example `int main = Increment 4` should give you an output of 5.

## Control flow with selection and recursion

Selection is offered using `if then else endif` clauses. This allows functions to produce different outputs depending on certain conditions, specified between the `if` and the `then`. For example, we can define a function that takes a bool and returns one of two numbers depending on whether it is true: `bool -> int From2Nos b = if b then 7 else 12 endif`. Selection can also be nested to form control flow in a function.

Looping can be achieved through controlled recursion within a selection clause. For example, a factorial function can be written as below by adding a base case in an if statement and calling itself with a decremented value.

```
int -> int Factorial n = if EqualTo n 1 then 1 else Multiply n (Factorial (Subtract n 1)) endif
```

Note the use of parantheses in the definition; the result of an expression involving multiple subexpressions must be passed as an argument to a function, so that expression must be contained in parantheses.

## Higher-order functions

It is possible to use functions as first-class citizens and pass them as arguments to other functions. Consider the factorial function above, where instead of multiplying each decreasing integer, we added them instead (triangle numbers), or indeed we apply any arbitrary function to them; this is a higher-order function.

The factorial function could be rewritten (and renamed to something more appropriate, e.g. `HOFunc`) to take an `int -> int -> int` function labelled `f` as an argument, such as `Multiply` or `Add`. In its definition, we replace `Multiply` with this new `f` and make sure to pass it on to the recursive call of `HOFunc`.

```
(int -> int -> int) -> int -> int HOFunc f n = if EqualTo n 1 then 1 else f n (HOFunc f (Subtract n 1)) endif
```

The factorial function could then be newly defined as `int -> int Factorial n = HOFunc Multiply n`, or a triangle number function as `int -> int TriangleNum n = HOFunc Add n`.
