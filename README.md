# functional-studio

IDE for functional programming. A-Level Computer Science NEA

NEA Documentation [here](https://github.com/pylasnier/fs-documentation).

## Getting started

To start writing a program, create a new file or open a file.

To interface with the program, you must define a variable `main`; this cannot be a function. Give it a type signature, assign it a value, and the output terminal will show you its value when you run the program.

Examples for `main` are `int main = 42`, `float main = 3.14`, or `bool main = true`.

## Writing your own function

To write your own function, assign it an appropriate type signature. For example, if we want to define a function that takes a number, adds 1 to it, and returns that new number, it will have a type signature of `int -> int`; it takes an `int` and returns an `int`.

The built-in addition function for integers is `int -> int -> int Add a b`, so to produce an output adding together the numbers 4 and 5 would be `int main = Add 4 5`. Note that math operators don't have infix alternatives, so `Add 4 5` cannot be replaced with `4 ``Add`` 5` or `4 + 5`. The same applies to all of the other available built-in operators.

Our own increment function will then be `int -> int Increment n = Add n 1`. You can test this function by passing it different values, for example `int main = Increment 4` should give you an output of 5.

## Control flow with selection and recursion

Selection is offered using `if then else endif` clauses. This allows functions to produce different outputs depending on certain conditions, specified between the `if` and the `then`. For example, we can define a function that takes a bool and returns one of two numbers depending on whether it is true: `bool -> int From2Nos b = if b then 7 else 12 endif`