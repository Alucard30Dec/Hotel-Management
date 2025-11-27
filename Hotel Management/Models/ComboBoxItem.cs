namespace HotelManagement
{
    // Class nhỏ để hiển thị (Text) trong ComboBox nhưng lưu (Value)
    public class ComboBoxItem
    {
        public object Value { get; set; }
        public string Text { get; set; }

        public override string ToString()
        {
            return Text; // ComboBox hiển thị Text
        }
    }
}
