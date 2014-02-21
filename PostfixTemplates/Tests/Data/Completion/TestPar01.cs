// ${COMPLETE_ITEM:par}

class C {
  C M() {
    C c = new C().M().pa{caret}
    return c;
  }
}