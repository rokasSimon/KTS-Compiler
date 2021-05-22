i32 main(i32 argc, string[] argv)
{
	i8[] array = new [6];

	array[0] = 'h';
	array[1] = 'e';
	array[2] = 'l';
	array[3] = 'l';
	array[4] = 'o';
	array[5] = 0;

	i32 x = 69;
	x = 420;

	printf("%s %d", array, x);

	return 0;
}