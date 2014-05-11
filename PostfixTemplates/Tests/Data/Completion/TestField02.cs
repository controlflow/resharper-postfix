// ${COMPLETE_ITEM:field}

class Person
{
  private Person mySomePerson;

  public Person(Person somePerson)
  {
    mySomePerson = somePerson;
    somePerson.{caret}
  }
}