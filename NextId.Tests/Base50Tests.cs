//namespace NextId.Tests;

//public class Base50Tests
//{
//    [Fact]
//    public void ConvertToNumber_ConvertBack_GivesSameValue()
//    {
//        for (int i = 0; i < 100; i++)
//        {
//            Thread.Sleep(15);

//            string value = UserId.NewId().Value.Split('-')[1];

//            string numberValue = Base50.GetNumberValue(value);
//            string value2 = Base50.GetStringValue(numberValue);

//            value2.Should().Be(value);
//        }
//    }
//}