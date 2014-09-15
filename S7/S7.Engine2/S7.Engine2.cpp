// S7.Engine2.cpp : Defines the entry point for the console application.
//

#include "stdafx.h"
#include <iostream>
#include <SDL.h>

int _tmain(int argc, _TCHAR* argv[])
{ 	
	if (SDL_Init(SDL_INIT_EVERYTHING) != 0){
		std::cout << "SDL_Init Error: " << SDL_GetError() << std::endl;
		return 1;
	}
	SDL_Quit();
	auto a = SDL_GetPrefPath("test", "testy");
	printf(a);
	SDL_DisplayMode mode;
	auto modes = SDL_GetNumDisplayModes(0);
	//printf( mode.w + " " +  mode.h);

	//SDL_assert(a == "n");
	//SDL_CreateWindow("", 0 ,0 , SDL_ge)

	return 0;
}

