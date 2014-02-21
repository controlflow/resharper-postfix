// ${COMPLETE_ITEM:cast}

class C {
  void M(System.Func<object, object> f) {
    M(x => x.{caret});
  }
}