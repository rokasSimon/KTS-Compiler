i32 main(i32 argc, string[] argv)
{
	string x = new [10];

	i32 start = 0;
	i32 end = 10;

	for (i32 i in [start..end))
	{
		x[i] = cast<i8>(i);
	}

	printf("%s", x);

	return 0;
}