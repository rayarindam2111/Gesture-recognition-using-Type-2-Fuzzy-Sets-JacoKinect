function [ obsDistance ] = distBW2obs( a, b )
%UNTITLED2 Summary of this function goes here
%   Detailed explanation goes here

    obsDistance = sqrt(sum((a-b).^2));
end

