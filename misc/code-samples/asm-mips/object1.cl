class Main inherits IO {
  main(): SELF_TYPE {
	let foo: Foo <- new Foo in
	  out_string("Hello, World.\n")
  };
};

class Foo {
};
