// This code snippet's origin is [a papaer](https://www.lume.ufrgs.br/bitstream/handle/10183/151038/001009883.pdf)
// from [LUME - the Digital Repository of the Universidade Federal do Rio Grande do Sul](https://www.lume.ufrgs.br/apresentacao)
class QuickSort() extends IO() {
  def quicksort(array: ArrayAny, lo: Int, hi: Int): Unit = {
    if (lo < hi) {
      var p: Int = partition(array, lo, hi);
      quicksort(array, lo, p - 1);
      quicksort(array, p + 1, hi)
    } else ()
  };
    
  def partition(array: ArrayAny, lo: Int, hi: Int): Int = {
    var pivot: Int = array.get(lo) match { case i: Int => i };
    var p: Int = lo;
    var i: Int = lo + 1;
    while (i <= hi) {
      if (((array.get(i)) match { case i: Int => i }) <= pivot)
        array_swap(array, i, { p = p + 1; p })
      else
        ();
      i = i + 1
    };
    
    array_swap(array, p, lo);
    p
  };
    
  def array_swap(array: ArrayAny, p: Int, q: Int): Unit = {
    var tmp: Any = array.get(p);
    array.set(p, array.get(q));
    array.set(q, tmp)
  };
    
  def out_array(array: ArrayAny): Unit = {
    var i: Int = 0;
    while (i < array.length()) {
      array.get(i) match {
        case i: Int => out_int(i); out_string(" ")
      };
        
      i = i + 1
    };
    
    out_nl()
  };
    
  {
    var array: ArrayAny = new ArrayAny(5);
    array.set(0, 30);
    array.set(1, 20);
    array.set(2, 50);
    array.set(3, 40);
    array.set(4, 10);
    
    out_array(array);
    
    quicksort(array, 0, array.length() - 1);
    
    out_array(array)
  };
}

class Main() {
  {
    new QuickSort()
  };
}

// DIAG: Build succeeded: Errors: 0. Warnings: 0
// OUT: 30 20 50 40 10
// OUT: 10 20 30 40 50
